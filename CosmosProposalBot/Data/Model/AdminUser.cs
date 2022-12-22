using System.ComponentModel.DataAnnotations;

namespace CosmosProposalBot.Data.Model;

public class AdminUser
{
    [Key]
    public Guid Id { get; set; }
        
    public Guild Guild { get; set; }
    public ulong UserId { get; set; }
}
