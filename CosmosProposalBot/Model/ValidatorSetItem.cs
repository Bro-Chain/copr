using Newtonsoft.Json;

namespace CosmosProposalBot.Model;

public class ValidatorSetItem
{
    [JsonProperty("address")]
    public string Address { get; set; }

    [JsonProperty("pub_key")]
    public ValidatorSetPublickey PublicKey { get; set; }
}
