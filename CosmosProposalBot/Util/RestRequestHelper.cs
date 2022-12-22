using CosmosProposalBot.Configuration;
using Newtonsoft.Json;
using Polly;

namespace CosmosProposalBot.Util;

public static class RestRequestHelper
{
    private static readonly JsonSerializerSettings JsonSerializerSettings = new ()
    {
        DateParseHandling = DateParseHandling.DateTime,
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        DateTimeZoneHandling = DateTimeZoneHandling.Utc
    };

    public static async Task<T?> RequestFromAnyWithRetry<T>( IHttpClientFactory httpClientFactory, IEnumerable<string> baseUrls, string path, BotOptions options )
    {
        foreach( var baseUrl in baseUrls )
        {
            var result = await RequestWithRetry<T>( httpClientFactory, baseUrl, path, options );
            if( result.Outcome == OutcomeType.Successful )
            {
                return result.Result;
            }
        }

        return default;
    }

    public static async Task<PolicyResult<T?>> RequestWithRetry<T>( IHttpClientFactory httpClientFactory, string baseUrl, string path, BotOptions options )
    {
        var httpClient = httpClientFactory.CreateClient(baseUrl);
        httpClient.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
        httpClient.Timeout = TimeSpan.FromSeconds( options.RestRequestTimeoutInSeconds );
        
        var txSearchResult = await Policy
            .Handle<HttpRequestException>()
            .Or<JsonReaderException>()
            .WaitAndRetryAsync(
                options.RestRequestRetriesPerEndpoint,
                _ => TimeSpan.FromSeconds(options.RestRequestRetryWaitInSeconds),
                onRetry: (exception, retryBackoff, _) =>
                {
                    Console.WriteLine($"Got exception with message '{exception.Message}' when calling API. Will retry in {retryBackoff}");
                })
            .ExecuteAndCaptureAsync(ExecuteApiCall);

        async Task<T?> ExecuteApiCall()
        {
            Console.WriteLine($"Calling {httpClient.BaseAddress}{path}");
            var result = await httpClient.GetAsync(path);
            var content = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>( content, JsonSerializerSettings );
        }

        return txSearchResult;
    }
}
