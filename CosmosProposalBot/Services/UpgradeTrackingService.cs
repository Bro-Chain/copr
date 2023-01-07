using System.Globalization;
using CosmosProposalBot.Configuration;
using CosmosProposalBot.Data;
using CosmosProposalBot.Data.Model;
using CosmosProposalBot.Model;
using CosmosProposalBot.Modules;
using CosmosProposalBot.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CosmosProposalBot.Services;

public class UpgradeTrackingService : IHostedService
{
    private readonly ILogger<UpgradeTrackingService> _logger;
    private readonly IHostEnvironment _env;
    private readonly IOptions<BotOptions> _botOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    public UpgradeTrackingService( 
        ILogger<UpgradeTrackingService> logger,
        IHostEnvironment env,
        IOptions<BotOptions> botOptions,
        IServiceProvider serviceProvider )
    {
        _logger = logger;
        _env = env;
        _botOptions = botOptions;
        _serviceProvider = serviceProvider;
    }
    
    public async Task StartAsync( CancellationToken cancellationToken )
    {
        Run( _cancellationTokenSource.Token );
    }
    
    private async void Run( CancellationToken cancellationToken )
    {
        await Task.Delay( 1000*10, cancellationToken );
        while ( !cancellationToken.IsCancellationRequested )
        {
            _logger.LogInformation( "{ServiceName} running at: {Time}", nameof(UpgradeTrackingService), DateTimeOffset.Now );
            try
            {
                await RunUpdate( cancellationToken );
            }
            catch( Exception e )
            {
                _logger.LogError($"Unhandled exception in {nameof(UpgradeTrackingService)}: {e}");
            }

            if( _env.IsDevelopment() )
            {
                await Task.Delay( 1000*10, cancellationToken );
            }
            else
            {
                await Task.Delay( 1000*60, cancellationToken );
            }
        }
    }

    private async Task RunUpdate( CancellationToken cancellationToken )
    {
        await using var outerScope = _serviceProvider.CreateAsyncScope();
        var outerDbContext = outerScope.ServiceProvider.GetRequiredService<CopsDbContext>();
        var httpClientFactory = outerScope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        var pendingUpgrades = outerDbContext.TrackedEvents
            .Where( te => te.Status == TrackedEventStatus.Pending );
        
        await Parallel.ForEachAsync( pendingUpgrades.Select( u => u.Id ).ToList(), cancellationToken, 
            async ( trackedEventId, token ) =>
            {
                try
                {
                    await using var innerScope = _serviceProvider.CreateAsyncScope();
                    var innerDbContext = innerScope.ServiceProvider.GetRequiredService<CopsDbContext>();
                    var trackedEvent = await innerDbContext.TrackedEvents
                        .Include( te => te.Threads )
                        .Include( te => te.Proposal )
                        .ThenInclude( p => p.Chain )
                        .ThenInclude( c => c.Endpoints )
                        .FirstOrDefaultAsync( te => te.Id == trackedEventId, token );
                    
                    var requestHelper = innerScope.ServiceProvider.GetRequiredService<ApiRequestHelper>();

                    var (latestSuccess, latestBlockHeader, _) = await requestHelper.GetBlockHeaderViaRest( httpClientFactory, trackedEvent.Proposal.Chain.Endpoints, trackedEvent.Proposal.Chain.Name, token );
                    if( !latestSuccess )
                    {
                        // try rpc 
                    }

                    var (historicalSuccess, historicalBlockHeader, _) = await requestHelper.GetBlockHeaderViaRest( httpClientFactory, trackedEvent.Proposal.Chain.Endpoints, trackedEvent.Proposal.Chain.Name, token, $"{latestBlockHeader.HeightNumerical - 1000L}" );
                    if( !historicalSuccess )
                    {
                        // try rpc 
                    }

                    var timePerBlock = ( latestBlockHeader.Time - historicalBlockHeader.Time ) / 1000.0;
                    var timeLeft = timePerBlock * ( trackedEvent.Height - latestBlockHeader.HeightNumerical );
                    
                    trackedEvent.HeightEstimatedAt = DateTime.UtcNow + timeLeft;
                    
                    if( !trackedEvent.NextNotificationAtSecondsLeft.HasValue ||
                        timeLeft.TotalSeconds < trackedEvent.NextNotificationAtSecondsLeft )
                    {
                        var eventBroadcaster = innerScope.ServiceProvider.GetRequiredService<EventBroadcaster>();
                        await eventBroadcaster.BroadcastUpgradeReminderAsync( trackedEvent );
                        trackedEvent.NextNotificationAtSecondsLeft = GetNextNotificationTime( trackedEvent.NextNotificationAtSecondsLeft );
                    }
                    
                    await innerDbContext.SaveChangesAsync(token);
                }
                catch( Exception e )
                {
                    _logger.LogWarning($"Failed in looking up block info: {e.Message}");
                }
            } );

        _logger.LogDebug($"Finished with all upgrades");
    }
    
    private long GetNextNotificationTime( long? currentNotificationAtSecondsLeft )
    {
        return currentNotificationAtSecondsLeft switch
        {
            60*60*24*2 => 60*60*24, // 2 days -> 1 day
            60*60*24 => 60*60*12, // 1 day -> 12 hours
            60*60*12 => 60*60*6, // 12 hours -> 6 hours
            60*60*6 => 60*60*3, // 6 hours -> 3 hours
            60*60*3 => 60*60, // 3 hours -> 1 hour
            60*60 => 60*15, // 1 hour -> 15 minutes
            60*15 => 60*5, // 15 minutes -> 5 minutes
            _ => 60*60*2424 // default to 24 hours, but...
                            // TODO: should be set based on vote period when tracked event is first set up
        };
    }

    public async Task StopAsync( CancellationToken cancellationToken )
    {
        _cancellationTokenSource.Cancel();
    }
}
