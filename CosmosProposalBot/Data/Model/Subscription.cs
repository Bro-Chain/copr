using System.ComponentModel.DataAnnotations;

namespace CosmosProposalBot.Data.Model;

public class Subscription
{
    [Key] 
    public Guid Id { get; set; }
    public Chain Chain { get; set; }
}
