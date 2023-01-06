using System.ComponentModel.DataAnnotations;
using CosmosProposalBot.Model;

namespace CosmosProposalBot.Data.Model;

public class TrackedEvent
{
    [Key] 
    public Guid Id { get; set; }
    public Proposal Proposal { get; set; }

    public ulong Height { get; set; }
    
    public DateTime? HeightEstimatedAt { get; set; }
    
    public DateTime? LastNotifiedAt { get; set; }

    public List<TrackedEventThread> Threads { get; set; } = new();
}
