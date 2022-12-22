using Newtonsoft.Json;

namespace CosmosProposalBot.Model;

public class ConsensusPublickey
{
    [JsonProperty("@type")]
    public string Type { get; set; }
    [JsonProperty("key")]
    public string Key { get; set; }
}
