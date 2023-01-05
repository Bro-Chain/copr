using CosmosProposalBot.Data;
using Discord;
using Microsoft.EntityFrameworkCore;

namespace CosmosProposalBot.Util;

public interface IPermissionHelper
{
    Task<bool> EnsureUserHasPermission( IInteractionContext context, CopsDbContext dbContext );
}

public class PermissionHelper : IPermissionHelper
{
    public async Task<bool> EnsureUserHasPermission( IInteractionContext context, CopsDbContext dbContext )
    {
        var userRoles = ( context.User as IGuildUser ).RoleIds;
        
        var guild = await dbContext.Guilds
            .Include( g => g.AdminRoles )
            .Include( g => g.AdminUsers )
            .SingleOrDefaultAsync( g => g.GuildId == context.Guild.Id );
        if( guild == default )
        {
            return false;
        }

        if( !guild.AdminRoles.Any() || 
            guild.AdminRoles.Any( r => userRoles.Contains( r.RoleId ) ) )
        {
            // We good
        }
        else if( !guild.AdminUsers.Any() || 
                 guild.AdminUsers.Any( u => u.UserId != context.User.Id ) )
        {
            // We good
        }
        else
        {
            return false;
        }

        return true;
    }
}
