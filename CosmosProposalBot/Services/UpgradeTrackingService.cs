using CosmosProposalBot.Util;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CosmosProposalBot.Services;

public class UpgradeTrackingService : IHostedService
{
    private readonly ILogger<UpgradeTrackingService> _logger;
    private readonly IHostEnvironment _env;
    private readonly IUpgradeTrackingRunner _upgradeTrackingRunner;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    public UpgradeTrackingService( 
        ILogger<UpgradeTrackingService> logger,
        IHostEnvironment env,
        IUpgradeTrackingRunner upgradeTrackingRunner )
    {
        _logger = logger;
        _env = env;
        _upgradeTrackingRunner = upgradeTrackingRunner;
    }
    
    public async Task StartAsync( CancellationToken cancellationToken )
    {
        // Allow Discord to start up.
        if( _env.IsProduction() )
        {
            await Task.Delay( 1000 * 60, cancellationToken );
        }
        _logger.LogInformation($"Starting ${nameof(UpgradeTrackingService)}");
        await Task.Delay( 1000*10, cancellationToken );
#pragma warning disable CS4014
        _upgradeTrackingRunner.RunAsync( _cancellationTokenSource.Token );
#pragma warning restore CS4014
    }

    public async Task StopAsync( CancellationToken cancellationToken )
    {
        _logger.LogInformation($"Stopping ${nameof(UpgradeTrackingService)}");
        _cancellationTokenSource.Cancel();
    }
}
