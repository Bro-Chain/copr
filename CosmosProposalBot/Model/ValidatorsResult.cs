using Newtonsoft.Json;

namespace CosmosProposalBot.Model;

public class ValidatorsResult
{
    [JsonProperty("validators")]
    public List<Validator> Validators { get; set; } = new();
}
