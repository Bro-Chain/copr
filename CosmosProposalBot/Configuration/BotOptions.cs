using System.ComponentModel.DataAnnotations;

namespace CosmosProposalBot.Configuration;

public class BotOptions
{
    [Required] 
    public string DiscordApiToken { get; set; }
    public ulong? DevelopmentGuildId { get; set; }

    public string? BlobStorageUrl { get; set; }
    public string? BlobStorageContainerName { get; set; }
    public string? BlobStorageSharedAccessToken { get; set; }

    public int RestRequestTimeoutInSeconds { get; set; }
    public int RestRequestRetriesPerEndpoint { get; set; }
    public int RestRequestRetryWaitInSeconds { get; set; }

    [Required] 
    public List<string> SupportedChains { get; set; } = new();
}
