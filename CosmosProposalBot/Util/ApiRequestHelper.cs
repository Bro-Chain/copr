using CosmosProposalBot.Data.Model;
using CosmosProposalBot.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CosmosProposalBot.Util;

public interface IApiRequestHelper
{
    Task<(bool, BlockInfoHeader?, Endpoint?)> GetBlockHeaderViaRest(
        IHttpClientFactory httpClientFactory,
        IReadOnlyList<Endpoint> endpoints,
        string chainName,
        CancellationToken token,
        string height = "latest" );
    Task<(bool, BlockInfoHeader?, Endpoint?)> GetBlockHeaderViaRpc(
        IHttpClientFactory httpClientFactory,
        IReadOnlyList<Endpoint> endpoints,
        string chainName,
        CancellationToken token,
        string height = "" );
}

public class ApiRequestHelper : IApiRequestHelper
{
    private readonly ILogger<ApiRequestHelper> _logger;

    public ApiRequestHelper( ILogger<ApiRequestHelper> logger )
    {
        _logger = logger;
    }

    public async Task<(bool, BlockInfoHeader?, Endpoint?)> GetBlockHeaderViaRest( 
        IHttpClientFactory httpClientFactory, 
        IReadOnlyList<Endpoint> endpoints, 
        string chainName,
        CancellationToken token,
        string height = "latest")
    {
        foreach( var restEndpoint in endpoints )
        {
            try
            {
                var restHttpClient = httpClientFactory.CreateClient( $"{restEndpoint.Provider}-rest" );
                restHttpClient.Timeout = TimeSpan.FromSeconds( 5 );
                
                var blockInfoJson = await restHttpClient.GetStringAsync( $"{restEndpoint.Url}/cosmos/base/tendermint/v1beta1/blocks/{height}", token );
                var blockInfo = JsonConvert.DeserializeObject<BlockInfoResult>( blockInfoJson );

                if( blockInfo?.Block?.Header?.Time < DateTime.UtcNow.AddMinutes( -5 ) )
                {
                    _logger.LogWarning( $"Block is too old ({blockInfo?.Block?.Header?.Time} < UTC NOW - 5min). Skipping {chainName} REST provider {restEndpoint.Provider}.." );
                }

                return (true, blockInfo.Block.Header, restEndpoint);
            }
            catch( Exception e )
            {
                continue;
            }
        }

        return ( false, null, null );
    }

    public async Task<(bool, BlockInfoHeader?, Endpoint?)> GetBlockHeaderViaRpc( 
        IHttpClientFactory httpClientFactory, 
        IReadOnlyList<Endpoint> endpoints, 
        string chainName,
        CancellationToken token,
        string height = "")
    {
        foreach( var restEndpoint in endpoints )
        {
            try
            {
                var restHttpClient = httpClientFactory.CreateClient( $"{restEndpoint.Provider}-rpc" );
                restHttpClient.Timeout = TimeSpan.FromSeconds( 5 );
                
                var blockInfoJson = await restHttpClient.GetStringAsync( $"{restEndpoint.Url}/block?height={height}", token );
                var blockInfo = JsonConvert.DeserializeObject<RpcBlockResponse>( blockInfoJson );

                if( blockInfo?.Result?.Block?.Header?.Time < DateTime.UtcNow.AddMinutes( -5 ) )
                {
                    _logger.LogWarning( $"Block is too old ({blockInfo?.Result?.Block?.Header?.Time} < UTC NOW - 5min). Skipping {chainName} REST provider {restEndpoint.Provider}.." );
                }

                return (true, blockInfo.Result?.Block.Header, restEndpoint);
            }
            catch( Exception e )
            {
                continue;
            }
        }

        return ( false, null, null );
    }
}
