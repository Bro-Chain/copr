using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CosmosProposalBot.Data.Model;
using Newtonsoft.Json;

namespace CosmosProposalBot.Model;

public class Proposal
{
    [Key] 
    public Guid Id { get; set; }

    [JsonProperty("proposal_id")]
    public string ProposalId { get; set; }
    public Chain Chain { get; set; }

    public string ProposalType { get; set; }
    public string Status { get; set; }
    public DateTime SubmitTime { get; set; }
    public DateTime DepositEndTime { get; set; }
    public DateTime VotingStartTime { get; set; }
    public DateTime VotingEndTime { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }

    [NotMapped]
    public string? Link => (!string.IsNullOrEmpty(this.Chain?.LinkPattern) ? $"{this.Chain.LinkPattern.TrimEnd('/')}/{ProposalId}" : null );

}
