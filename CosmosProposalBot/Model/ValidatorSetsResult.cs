using Newtonsoft.Json;

namespace CosmosProposalBot.Model;

public class ValidatorSetsResult
{
    [JsonProperty("result")]
    public ValidatorSet Result { get; set; }
}
