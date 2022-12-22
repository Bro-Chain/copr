using Newtonsoft.Json;

namespace CosmosProposalBot.Model;

public class ValidatorDescription
{
    [JsonProperty("moniker")]
    public string Moniker { get; set; }
}
