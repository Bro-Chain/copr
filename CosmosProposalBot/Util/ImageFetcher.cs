using CosmosProposalBot.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CosmosProposalBot.Util;

public interface IImageFetcher
{
    Task<string?> FetchImage( string? sourceUrl, string chainName );
}

public class ImageFetcher : IImageFetcher
{
    private readonly ILogger<ImageFetcher> _logger;
    private readonly IOptions<BotOptions> _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly bool _isEnabled = true;

    public ImageFetcher(
        ILogger<ImageFetcher> logger,
        IOptions<BotOptions> options,
        IHttpClientFactory httpClientFactory,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _options = options;
        _httpClientFactory = httpClientFactory;
        _serviceProvider = serviceProvider;

        if( string.IsNullOrEmpty( _options.Value.BlobStorageUrl ) ||
            string.IsNullOrEmpty( _options.Value.BlobStorageContainerName ) ||
            string.IsNullOrEmpty( _options.Value.BlobStorageSharedAccessToken ) )
        {
            _logger.LogInformation("Blob storage configuration is missing; {ServiceName} will not be available", nameof(ImageFetcher));
            _isEnabled = false;
        }
    }

    public async Task<string?> FetchImage( string? sourceUrl, string chainName )
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

        var client = _httpClientFactory.CreateClient(_options.Value.BlobStorageUrl!);
        var fullUrl = $"{_options.Value.BlobStorageUrl!}/{_options.Value.BlobStorageContainerName!}/{chainName}.png";
        var result = await client.GetAsync(fullUrl, HttpCompletionOption.ResponseHeadersRead);
        if( result.IsSuccessStatusCode )
        {
            return fullUrl;
        }
        
        return default;
    }
}
