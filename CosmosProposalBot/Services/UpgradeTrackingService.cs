using System.Globalization;
using CosmosProposalBot.Configuration;
using CosmosProposalBot.Data;
using CosmosProposalBot.Data.Model;
using CosmosProposalBot.Model;
using CosmosProposalBot.Modules;
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
                await Task.Delay( 1000*60*5, cancellationToken );
            }
        }
    }

    private async Task RunUpdate( CancellationToken cancellationToken )
    {
        await using var outerScope = _serviceProvider.CreateAsyncScope();
        var outerDbContext = outerScope.ServiceProvider.GetRequiredService<CopsDbContext>();
        var httpClientFactory = outerScope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        var chains = outerDbContext.Chains
            .Where(c => _botOptions.Value.SupportedChains.Contains( c.Name ) || c.CustomForGuildId != null )
            .ToList();
        
        await Parallel.ForEachAsync( chains.Select( c => c.Id ).ToList(), cancellationToken, 
            async ( chainId, token ) =>
            {
                // try
                // {
                //     await using var innerScope = _serviceProvider.CreateAsyncScope();
                //     var innerDbContext = innerScope.ServiceProvider.GetRequiredService<CopsDbContext>();
                //     var chain = await innerDbContext.Chains
                //         .Include( c => c.Endpoints )
                //         .Include( c => c.Proposals )
                //         .SingleAsync( c => c.Id == chainId, token );
                //
                //     _logger.LogInformation("Updating proposals for chain {ChainName}", chain.Name);
                //     
                //     var eventBroadcaster = innerScope.ServiceProvider.GetRequiredService<EventBroadcaster>();
                //     var updatedFromVerifiedUpToDateNode = await UpdateProps(chain, innerDbContext, httpClientFactory, eventBroadcaster, token);
                //     if (!updatedFromVerifiedUpToDateNode)
                //     {
                //         _logger.LogWarning("Could not update proposals for chain {ChainName} from verified up to date node, trying from any node", chain.Name);
                //         await UpdateProps(chain, innerDbContext, httpClientFactory, eventBroadcaster, token, true);
                //     } 
                //     
                //     _logger.LogTrace($"Finished with chain {chain.Name}");
                // }
                // catch( Exception e )
                // {
                //     _logger.LogWarning($"Failed in looking up block info for chain id {chainId}: {e.Message}");
                // }
            } );

        _logger.LogDebug($"Finished with all chains");
    }

    public async Task StopAsync( CancellationToken cancellationToken )
    {
        _cancellationTokenSource.Cancel();
    }
}
