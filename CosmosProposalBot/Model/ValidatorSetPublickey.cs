using Newtonsoft.Json;

namespace CosmosProposalBot.Model;

public class ValidatorSetPublickey
{
    [JsonProperty("type")]
    public string Type { get; set; }
    [JsonProperty("value")]
    public string Key { get; set; }
}
