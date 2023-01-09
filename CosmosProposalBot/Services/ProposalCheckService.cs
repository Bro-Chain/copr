using CosmosProposalBot.Util;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CosmosProposalBot.Services;

public class ProposalCheckService : IHostedService
{
    private readonly ILogger<ProposalCheckService> _logger;
    private readonly IProposalCheckRunner _proposalCheckRunner;
    private readonly IHostEnvironment _env;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    public ProposalCheckService( 
        ILogger<ProposalCheckService> logger,
        IProposalCheckRunner proposalCheckRunner,
        IHostEnvironment env )
    {
        _logger = logger;
        _proposalCheckRunner = proposalCheckRunner;
        _env = env;
    }
    
    public async Task StartAsync( CancellationToken cancellationToken )
    {
        // Allow Discord to start up.
        if( _env.IsProduction() )
        {
            await Task.Delay( 1000 * 60, cancellationToken );
        }
        _logger.LogInformation($"Starting ${nameof(ProposalCheckService)}");
        
#pragma warning disable CS4014
        _proposalCheckRunner.RunAsync( _cancellationTokenSource.Token );
#pragma warning restore CS4014
    }

    public async Task StopAsync( CancellationToken cancellationToken )
    {
        _logger.LogInformation($"Stopping ${nameof(ProposalCheckService)}");
        _cancellationTokenSource.Cancel();
    }
}
