using System.ComponentModel.DataAnnotations;

namespace CosmosProposalBot.Data.Model;

public class Guild
{
    [Key]
    public Guid Id { get; set; }
        
    public ulong GuildId { get; set; }
    public List<AdminUser> AdminUsers { get; set; } = new ();
    public List<AdminRole> AdminRoles { get; set; } = new ();
    
}
