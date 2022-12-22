using Azure;
using Azure.Storage.Blobs;
using CosmosProposalBot.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CosmosProposalBot.Services;

public class ImageFetcher
{
    private readonly ILogger<ImageFetcher> _logger;
    private readonly IOptions<BotOptions> _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly bool _isEnabled = true;

    public ImageFetcher(
        ILogger<ImageFetcher> logger,
        IOptions<BotOptions> options,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _options = options;
        _serviceProvider = serviceProvider;

        if( string.IsNullOrEmpty( _options.Value.BlobStorageUrl ) ||
            string.IsNullOrEmpty( _options.Value.BlobStorageContainerName ) ||
            string.IsNullOrEmpty( _options.Value.BlobStorageSharedAccessToken ) )
        {
            _logger.LogInformation("Blob storage configuration is missing; {ServiceName} will not be available", nameof(ImageFetcher));
            _isEnabled = false;
        }
    }

    public async Task<string?> FetchImage( string sourceUrl, string chainName )
    {
        if( !_isEnabled )
        {
            return sourceUrl;
        }
        
        if( string.IsNullOrEmpty( sourceUrl ) )
        {
            _logger.LogError($"Source URL is empty");
            return default;
        }

        if( sourceUrl.EndsWith( ".png" ) )
        {
            _logger.LogDebug("{ChainName} Source image is already a png, returning it...", chainName);
            return sourceUrl;
        }

        var blobClient = CreateBlobServiceClient( chainName );
        if( await blobClient.ExistsAsync() )
        {
            _logger.LogDebug("Returning cached {ChainName} image, '{Url}'", chainName, blobClient.Uri);
            return blobClient.Uri.ToString();
        }
        return default;
    }

    private BlobClient CreateBlobServiceClient( string filename )
    {
        var serviceClient = new BlobServiceClient( 
            new Uri( _options.Value.BlobStorageUrl! )
            , new AzureSasCredential( _options.Value.BlobStorageSharedAccessToken! ) );

        var containerClient = serviceClient.GetBlobContainerClient( _options.Value.BlobStorageContainerName );
        return containerClient.GetBlobClient( $"{filename}.png" );
    }
}
