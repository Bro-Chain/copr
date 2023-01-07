using System.Globalization;
using CosmosProposalBot.Configuration;
using CosmosProposalBot.Data;
using CosmosProposalBot.Data.Model;
using CosmosProposalBot.Model;
using CosmosProposalBot.Modules;
using CosmosProposalBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CosmosProposalBot.Util;

public interface IProposalCheckRunner
{
    Task RunAsync( CancellationToken token );
}

public class ProposalCheckRunner : IProposalCheckRunner
{
    private readonly ILogger<ProposalCheckRunner> _logger;
    private readonly IHostEnvironment _env;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<BotOptions> _botOptions;

    public ProposalCheckRunner( 
        ILogger<ProposalCheckRunner> logger,
        IHostEnvironment env,
        IServiceProvider serviceProvider,
        IOptions<BotOptions> botOptions )
    {
        _logger = logger;
        _env = env;
        _serviceProvider = serviceProvider;
        _botOptions = botOptions;
    }

    public async Task RunAsync( CancellationToken cancellationToken )
    {

        while ( !cancellationToken.IsCancellationRequested )
        {
            _logger.LogInformation( "{ServiceName} running at: {Time}", nameof(ProposalCheckService), DateTimeOffset.Now );
            try
            {
                await RunUpdate( cancellationToken );
            }
            catch( Exception e )
            {
                _logger.LogError($"Unhandled exception in {nameof(ProposalCheckService)}: {e}");
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
            .Where( c => c.Name == "gravitybridge")
            .Where(c => _botOptions.Value.SupportedChains.Contains( c.Name ) || c.CustomForGuildId != null )
            .ToList();
        
        await Parallel.ForEachAsync( chains.Select( c => c.Id ).ToList(), cancellationToken, 
            async ( chainId, token ) =>
            {
                try
                {
                    await using var innerScope = _serviceProvider.CreateAsyncScope();
                    var innerDbContext = innerScope.ServiceProvider.GetRequiredService<CopsDbContext>();
                    var chain = await innerDbContext.Chains
                        .Include( c => c.Endpoints )
                        .Include( c => c.Proposals )
                        .SingleAsync( c => c.Id == chainId, token );

                    _logger.LogInformation("Updating proposals for chain {ChainName}", chain.Name);
                    
                    var eventBroadcaster = innerScope.ServiceProvider.GetRequiredService<IEventBroadcaster>();
                    var updatedFromVerifiedUpToDateNode = await UpdateProps(chain, innerDbContext, httpClientFactory, eventBroadcaster, token);
                    if (!updatedFromVerifiedUpToDateNode)
                    {
                        _logger.LogWarning("Could not update proposals for chain {ChainName} from verified up to date node, trying from any node", chain.Name);
                        await UpdateProps(chain, innerDbContext, httpClientFactory, eventBroadcaster, token, true);
                    } 
                    
                    _logger.LogTrace($"Finished with chain {chain.Name}");
                }
                catch( Exception e )
                {
                    _logger.LogWarning($"Failed in looking up block info for chain id {chainId}: {e.Message}");
                }
            } );

        _logger.LogDebug($"Finished with all chains");
    }    
    
    private async Task<bool> UpdateProps( 
        Chain chain, 
        CopsDbContext innerDbContext, 
        IHttpClientFactory httpClientFactory, 
        IEventBroadcaster eventBroadcaster, 
        CancellationToken token,
        bool skipVerifiedUpToDateNode = false )
    {
        foreach( var restEndpoint in chain.Endpoints.Where( e => e.Type == EndpointType.Rest ) )
        {
            try
            {
                var restHttpClient = httpClientFactory.CreateClient( $"{restEndpoint.Provider}-rest" );

                if( !skipVerifiedUpToDateNode )
                {
                    var latestBlockInfoJson = await restHttpClient.GetStringAsync( $"{restEndpoint.Url}/cosmos/base/tendermint/v1beta1/blocks/latest", token );
                    var latestBlockInfo = JsonConvert.DeserializeObject<BlockInfoResult>( latestBlockInfoJson );

                    if( latestBlockInfo?.Block?.Header?.Time < DateTime.UtcNow.AddMinutes( -5 ) )
                    {
                        _logger.LogWarning( $"Block is too old ({latestBlockInfo?.Block?.Header?.Time} < UTC NOW - 5min). Skipping {chain.Name} REST provider {restEndpoint.Provider}.." );
                        continue;
                    }
                }

                var proposalInfoJson =
                    await restHttpClient.GetStringAsync( $"{restEndpoint.Url}/cosmos/gov/v1beta1/proposals?pagination.limit=25&pagination.reverse=true&pagination.key=", token );
                var propInfos = JsonConvert.DeserializeObject<ProposalInfoResponse>( proposalInfoJson );

                foreach( var prop in propInfos.Proposals )
                {
                    var existingProposal = chain.Proposals.FirstOrDefault( p => p.ProposalId == prop.Id );
                    if( existingProposal != null )
                    {
                        if( existingProposal.Status != prop.Status )
                        {
                            _logger.LogInformation( "Updating status of proposal {ProposalId} on chain {ChainName} from {OldStatus} to {NewStatus}", 
                                prop.Id, 
                                chain.Name,
                                existingProposal.Status, 
                                prop.Status );
                            
                            await eventBroadcaster.BroadcastStatusChangeAsync( existingProposal, prop.Status );
                            existingProposal.Status = prop.Status;
                            existingProposal.SubmitTime = DateTime.Parse( prop.SubmitTime, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal ).ToUniversalTime();
                            existingProposal.DepositEndTime =
                                DateTime.Parse( prop.DepositEndTime, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal ).ToUniversalTime();
                            existingProposal.VotingStartTime =
                                DateTime.Parse( prop.VotingStartTIme, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal ).ToUniversalTime();
                            existingProposal.VotingEndTime =
                                DateTime.Parse( prop.VotingEndTime, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal ).ToUniversalTime();
                            
                            await eventBroadcaster.BroadcastNewUpgradeAsync( existingProposal, prop.Status, prop.Content.Plan );
                        }
                    }
                    else
                    {
                        _logger.LogInformation( "Found new proposal {ProposalId} on chain {ChainName}", prop.Id, chain.Name );
                        var newProp = new Proposal
                        {
                            Chain = chain,
                            Title = prop.Content.Title,
                            Description = prop.Content.Description,
                            ProposalId = prop.Id,
                            ProposalType = prop.Content.Type,
                            SubmitTime = DateTime.Parse( prop.SubmitTime, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal ).ToUniversalTime(),
                            DepositEndTime = DateTime.Parse( prop.DepositEndTime, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal ).ToUniversalTime(),
                            VotingStartTime = DateTime.Parse( prop.VotingStartTIme, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal ).ToUniversalTime(),
                            VotingEndTime = DateTime.Parse( prop.VotingEndTime, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal ).ToUniversalTime()
                        };
                        await eventBroadcaster.BroadcastStatusChangeAsync( newProp, prop.Status );
                        newProp.Status = prop.Status;

                        chain.Proposals.Add( newProp );
                        innerDbContext.Proposals.Add( newProp );
                        
                        await eventBroadcaster.BroadcastNewUpgradeAsync( newProp, prop.Status, prop.Content.Plan );
                    }
                }

                await innerDbContext.SaveChangesAsync( token );
                return true;
            }
            catch( Exception e )
            {
                _logger.LogDebug( $"Failed in looking up block info for chain {chain.Name}: {e.Message}. Will try with the next RPC endpoint if there is one..." );
            }
        }

        return false;
    }
}
