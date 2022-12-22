using CosmosProposalBot.Data.Model;
using Newtonsoft.Json;

namespace CosmosProposalBot.Model;

public class ProposalInfoContent
{
    [JsonProperty("@type")]
    public string Type { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public ProposalInfoUpgradePlan Plan { get; set; }
}
