using System.ComponentModel.DataAnnotations;
using CosmosProposalBot.Model;

namespace CosmosProposalBot.Data.Model;

public enum TrackedEventStatus
{
    Pending,
    Passed
}

public class TrackedEvent
{
    [Key] 
    public Guid Id { get; set; }
    public Proposal Proposal { get; set; }

    public ulong Height { get; set; }
    public TrackedEventStatus Status { get; set; }
    
    public DateTime? HeightEstimatedAt { get; set; }
    
    public long? NextNotificationAtSecondsLeft { get; set; }

    public List<TrackedEventThread> Threads { get; set; } = new();
}
