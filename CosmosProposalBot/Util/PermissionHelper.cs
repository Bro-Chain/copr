using CosmosProposalBot.Data;
using CosmosProposalBot.Data.Model;
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
            // There is no record of this guild in the database, so any user can configure the bot
            return true;
        }

        if( !AnyAdminRolesOrUsersDefined( guild ) || 
            UserHasAdminRole( guild, userRoles ) || 
            UserIsAssignedAdmin( guild, context.User.Id ) )
        {
            return true;
        }
        return false;
    }

    private static bool AnyAdminRolesOrUsersDefined( Guild guild ) 
        => guild.AdminRoles.Any() || guild.AdminUsers.Any();

    private static bool UserHasAdminRole( Guild guild, IReadOnlyCollection<ulong> userRoleIds )
        => guild.AdminRoles.Any( r => userRoleIds.Contains( r.RoleId ) );

    private static bool UserIsAssignedAdmin( Guild guild, ulong userId )
        => guild.AdminUsers.Any( u => u.UserId == userId );
}
