using CosmosProposalBot.Data.Model;
using CosmosProposalBot.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CosmosProposalBot.Util;

public class ApiRequestHelper
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
                
                var latestBlockInfoJson = await restHttpClient.GetStringAsync( $"{restEndpoint.Url}/cosmos/base/tendermint/v1beta1/blocks/{height}", token );
                var latestBlockInfo = JsonConvert.DeserializeObject<BlockInfoResult>( latestBlockInfoJson );

                if( latestBlockInfo?.Block?.Header?.Time < DateTime.UtcNow.AddMinutes( -5 ) )
                {
                    _logger.LogWarning( $"Block is too old ({latestBlockInfo?.Block?.Header?.Time} < UTC NOW - 5min). Skipping {chainName} REST provider {restEndpoint.Provider}.." );
                }

                return (true, latestBlockInfo.Block.Header, restEndpoint);
            }
            catch( Exception e )
            {
                continue;
            }
        }

        return ( false, null, null );
    }
}
