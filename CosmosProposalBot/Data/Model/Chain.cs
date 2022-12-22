using System.ComponentModel.DataAnnotations;
using CosmosProposalBot.Model;

namespace CosmosProposalBot.Data.Model;

public class Chain
{
    [Key] 
    public Guid Id { get; set; }
    public string Name { get; set; }
    public List<Endpoint> Endpoints { get; set; } = new ();
    public List<Proposal> Proposals { get; set; } = new ();
    public string? ImageUrl { get; set; }
    public ulong? CustomForGuildId { get; set; }
    
    public string? LinkPattern { get; set; }
}
