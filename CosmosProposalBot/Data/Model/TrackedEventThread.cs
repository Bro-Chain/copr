using System.ComponentModel.DataAnnotations;

namespace CosmosProposalBot.Data.Model;

public class TrackedEventThread
{
    [Key] 
    public Guid Id { get; set; }
    public TrackedEvent TrackedEvent { get; set; }

    public ulong ThreadId { get; set; }
    
    public ulong GuildId { get; set; }
}
