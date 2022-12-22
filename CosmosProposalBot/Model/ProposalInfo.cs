using CosmosProposalBot.Data.Model;
using Newtonsoft.Json;

namespace CosmosProposalBot.Model;

public class ProposalInfo
{
    [JsonProperty("proposal_id")]
    public string Id { get; set; }

    public ProposalInfoContent Content { get; set; }
    public string Status { get; set; }
    [JsonProperty("submit_time")]
    public string SubmitTime { get; set; }
    [JsonProperty("deposit_end_time")]
    public string DepositEndTime { get; set; }
    [JsonProperty("voting_start_time")]
    public string VotingStartTIme { get; set; }
    [JsonProperty("voting_end_time")]
    public string VotingEndTime { get; set; }
}
