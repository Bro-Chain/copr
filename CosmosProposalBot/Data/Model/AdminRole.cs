using System.ComponentModel.DataAnnotations;

namespace CosmosProposalBot.Data.Model;

public class AdminRole
{
    [Key]
    public Guid Id { get; set; }
        
    public Guild Guild { get; set; }
    public ulong RoleId { get; set; }
}
