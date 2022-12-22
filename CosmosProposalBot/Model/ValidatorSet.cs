using Newtonsoft.Json;

namespace CosmosProposalBot.Model;

public class ValidatorSet
{
    [JsonProperty("block_height")]
    public string BlockHeight { get; set; }
    [JsonProperty("validators")]
    public List<ValidatorSetItem> Validators { get; set; } = new ();
}
