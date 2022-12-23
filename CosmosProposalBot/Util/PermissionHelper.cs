using CosmosProposalBot.Data;
using Discord;
using Microsoft.EntityFrameworkCore;

namespace CosmosProposalBot.Util;

public class PermissionHelper
{
    public async Task<bool> EnsureUserHasPermission( IInteractionContext context, CopsDbContext dbContext )
    {
        var userRoles = ( context.User as IGuildUser ).RoleIds;
        
        var guild = await dbContext.Guilds
            .Include( g => g.AdminRoles )
            .Include( g => g.AdminUsers )
            .SingleOrDefaultAsync( g => g.GuildId == context.Guild.Id );
        if( guild != default )
        {
            if( !guild.AdminRoles.Any() || 
                guild.AdminRoles.Any( r => userRoles.Contains( r.RoleId ) ) )
            {
                // We good
            }
            else if( !guild.AdminRoles.Any() || 
                     guild.AdminUsers.Any( u => u.UserId != context.User.Id ) )
            {
                // We good
            }
            else
            {
                return false;
            }                
        }

        return true;
    }
}
