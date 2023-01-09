using CosmosProposalBot.Data;
using CosmosProposalBot.Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CosmosProposalBot;

public class ChainList
{
    public List<string> Chains { get; set; }
}

public class ChainApiEndpoint
{
    public string Address { get; set; }
    public string Provider { get; set; }
}

public class ChainApis
{
    public List<ChainApiEndpoint> Rpc { get; set; } = new ();
    public List<ChainApiEndpoint> Rest { get; set; } = new ();
    public List<ChainApiEndpoint> Grpc { get; set; } = new ();
}

public class ChainInfo
{
    public ChainApis Apis { get; set; } = new();
}

public class ChainListChainInfo
{
    [JsonProperty("pretty_name")]
    public string Name { get; set; }
    public string Path { get; set; }
    [JsonProperty("chain_id")]
    public string ChainId { get; set; }
    public string Status { get; set; }
    public string Image { get; set; }
}

public class ChainListResponse
{
    public List<ChainListChainInfo> Chains { get; set; }
}

public class UpdateChainListService : IHostedService
{
    private readonly ILogger<UpdateChainListService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    public UpdateChainListService( ILogger<UpdateChainListService> logger,
        IServiceProvider serviceProvider )
    {
        _logger = logger;
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
            _logger.LogInformation( "{ServiceName} running at: {Time}", nameof(UpdateChainListService), DateTimeOffset.Now );
            await RunUpdate( cancellationToken );
            
            await Task.Delay( 1000*60*60, cancellationToken );
        }
    }

    private async Task RunUpdate( CancellationToken cancellationToken )
    {
        var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<CopsDbContext>();
        
        var httpClientFactory = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var directoryHttpClient = httpClientFactory.CreateClient( "chainList" );

        var chainListJson = await directoryHttpClient.GetStringAsync( "https://chains.cosmos.directory/", cancellationToken );
        var chainList = JsonConvert.DeserializeObject<ChainListResponse>( chainListJson );

        var chainRegistryHttpClient = httpClientFactory.CreateClient( "chainInfos" );
        foreach( var chainListInfo in chainList.Chains )
        {
            try
            {
                var chainName = chainListInfo.Path;
                var chainInfoJson = await chainRegistryHttpClient.GetStringAsync( $"https://raw.githubusercontent.com/cosmos/chain-registry/master/{chainName}/chain.json", cancellationToken );
                var chainInfo = JsonConvert.DeserializeObject<ChainInfo>( chainInfoJson );
                
                _logger.LogDebug( "Chain: {ChainName} has {RpcCount} RPC endpoints", chainName, chainInfo.Apis.Rpc.Count );

                var chain = await dbContext.Chains
                    .Include( c => c.Endpoints )
                    .FirstOrDefaultAsync( c => c.Name == chainName, cancellationToken );
                if( chain == default )
                {
                    chain = new Chain
                    {
                        Name = chainName,
                        ChainId = chainListInfo.ChainId,
                        ImageUrl = chainListInfo.Image
                    };
                    dbContext.Chains.Add( chain );

                    var restEndpoints = chainInfo.Apis.Rest.Where(e => !string.IsNullOrEmpty(e.Provider)).Select( e => new Endpoint
                        {
                            Chain = chain,
                            Type = EndpointType.Rest,
                            Url = e.Address.StartsWith("http") ? e.Address : $"https://{e.Address}",
                            Provider = e.Provider
                        } )
                        .ToList();
                    chain.Endpoints.AddRange( restEndpoints );
                    dbContext.Endpoints.AddRange( restEndpoints );
                    
                    var rpcEndpoints = chainInfo.Apis.Rpc.Where(e => !string.IsNullOrEmpty(e.Provider)).Select( e => new Endpoint
                        {
                            Chain = chain,
                            Type = EndpointType.Rpc,
                            Url = e.Address.StartsWith("http") ? e.Address : $"https://{e.Address}",
                            Provider = e.Provider
                        } )
                        .ToList();
                    chain.Endpoints.AddRange( rpcEndpoints );
                    dbContext.Endpoints.AddRange( rpcEndpoints );
                }
                else
                {
                    chain.ImageUrl = chainListInfo.Image;
                    chain.ChainId = chainListInfo.ChainId;
                    foreach( var endpoint in chainInfo.Apis.Rest.Where( e => !string.IsNullOrEmpty(e.Provider)) )
                    {
                        var existingEndpoint = chain.Endpoints.FirstOrDefault( e => e.Provider == endpoint.Provider 
                                                                                    && e.Type == EndpointType.Rest );
                        if( existingEndpoint == default )
                        {
                            var newEndpoint = new Endpoint
                            {
                                Chain = chain,
                                Type = EndpointType.Rest,
                                Url = endpoint.Address.StartsWith("http") ? endpoint.Address : $"https://{endpoint.Address}",
                                Provider = endpoint.Provider
                            };
                            chain.Endpoints.Add(newEndpoint );
                            dbContext.Endpoints.Add( newEndpoint );
                        }
                        else
                        {
                            existingEndpoint.Url = endpoint.Address.StartsWith( "http" ) ? endpoint.Address : $"https://{endpoint.Address}";
                        }
                    }
                    
                    foreach( var endpoint in chainInfo.Apis.Rpc.Where( e => !string.IsNullOrEmpty(e.Provider)) )
                    {
                        var existingEndpoint = chain.Endpoints.FirstOrDefault( e => e.Provider == endpoint.Provider 
                                                                                    && e.Type == EndpointType.Rpc );
                        if( existingEndpoint == default )
                        {
                            var newEndpoint = new Endpoint
                            {
                                Chain = chain,
                                Type = EndpointType.Rpc,
                                Url = endpoint.Address.StartsWith("http") ? endpoint.Address : $"https://{endpoint.Address}",
                                Provider = endpoint.Provider
                            };
                            chain.Endpoints.Add( newEndpoint);
                            dbContext.Endpoints.Add( newEndpoint );
                        }
                        else
                        {
                            existingEndpoint.Url = endpoint.Address.StartsWith( "http" ) ? endpoint.Address : $"https://{endpoint.Address}";
                        }
                    }
                }

                await dbContext.SaveChangesAsync( cancellationToken );
                _logger.LogDebug($"Updated all endpoints for {chain.Name}");
            }
            catch( Exception e )
            {
                _logger.LogWarning($"Failed in looking up endpoints for chain {chainListInfo.Path}: {e.Message}");
            }
        }
        _logger.LogInformation($"Updated all endpoints");
    }

    public async Task StopAsync( CancellationToken cancellationToken )
    {
        _cancellationTokenSource.Cancel();
    }
}
