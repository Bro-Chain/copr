using CosmosProposalBot.Configuration;
using CosmosProposalBot.Data;
using CosmosProposalBot.Data.Model;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CosmosProposalBot.Util;

public interface IConfigModuleActions
{
    Task AllowRoleAsync( IInteractionContext context, IRole role );
    Task RevokeRoleAsync( IInteractionContext context, IRole role );
    Task ListRolesAsync( IInteractionContext context);
    Task AddEndpointAsync( IInteractionContext context);
    Task RemoveEndpointAsync( IInteractionContext context, string chainName, string providerName );
    Task AddCustomChainAsync( IInteractionContext context);
    Task RemoveCustomChainAsync( IInteractionContext context, string chainName );
}

public class ConfigModuleActions : IConfigModuleActions
{
    private readonly ILogger<ConfigModuleActions> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ConfigModuleActions( ILogger<ConfigModuleActions> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    private static async Task<CopsDbContext?> InitAction( AsyncServiceScope scope, IInteractionContext context )
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<CopsDbContext>();
        var permissionHelper = scope.ServiceProvider.GetRequiredService<IPermissionHelper>();

        if( !await permissionHelper.EnsureUserHasPermission( context, dbContext ) )
        {
            await context.Interaction.FollowupAsync( "You do not have permission to use this command", ephemeral: true );
            return default;
        }

        return dbContext;
    }
    
    public async Task AllowRoleAsync( IInteractionContext context, IRole role )
    {
        try
        {
            await context.Interaction.DeferAsync( ephemeral: true );

            await using var scope = _serviceProvider.CreateAsyncScope();
            var dbContext = await InitAction(scope, context);
            if( dbContext == null ) return;

            var guild = await dbContext.Guilds
                .AsSplitQuery()
                .Include( g => g.AdminRoles )
                .SingleOrDefaultAsync( g => g.GuildId == context.Guild.Id );
            if( guild == default )
            {
                guild = new Guild()
                {
                    GuildId = role.Guild.Id
                };
                dbContext.Guilds.Add( guild );
            }

            if( guild.AdminRoles.All( r => r.RoleId != role.Id ) )
            {
                var newRole = new AdminRole()
                {
                    Guild = guild,
                    RoleId = role.Id
                };    
                guild.AdminRoles.Add( newRole );
                await dbContext.SaveChangesAsync();
                await context.Interaction.FollowupAsync( $"Role @{role.Name} was **added** as admin group", ephemeral: true );
            }
            else
            {
                await dbContext.SaveChangesAsync();
                await context.Interaction.FollowupAsync( $"Role @{role.Name} is **already** an admin group", ephemeral: true );
            }
        }
        catch( Exception e )
        {
            _logger.LogError(e.Message);
            await context.Interaction.RespondAsync( $"Something went wrong. Please contact the my developers and let them know what happened.", ephemeral: true );
        }
    }

    public async Task RevokeRoleAsync( IInteractionContext context, IRole role )
    {
        try
        {
            await context.Interaction.DeferAsync( ephemeral: true );

            await using var scope = _serviceProvider.CreateAsyncScope();
            var dbContext = await InitAction(scope, context);
            if( dbContext == null ) return;

            var guild = await dbContext.Guilds
                .Include( g => g.AdminRoles )
                .SingleOrDefaultAsync( g => g.GuildId == role.Guild.Id );
            if( guild == default )
            {
                await context.Interaction.FollowupAsync( $"Role @{role.Name} **is not** an admin group", ephemeral: true );
                return;
            }

            var existingRole = guild.AdminRoles.FirstOrDefault( r => r.RoleId == role.Id );
            if( existingRole == default )
            {
                await context.Interaction.FollowupAsync( $"Role @{role.Name} **is not** an admin group", ephemeral: true );
                return;
            }

            guild.AdminRoles.RemoveAll( r => r.RoleId == role.Id );
            await context.Interaction.FollowupAsync( $"Role @{role.Name} was **revoked** as admin group", ephemeral: true );
            
            await dbContext.SaveChangesAsync();
        }
        catch( Exception e )
        {
            _logger.LogError(e.Message);
            await context.Interaction.RespondAsync( $"Something went wrong. Please contact the my developers and let them know what happened.", ephemeral: true );
        }
    }

    public async Task ListRolesAsync( IInteractionContext context)
    {
        try
        {
            await context.Interaction.DeferAsync( ephemeral: true );

            await using var scope = _serviceProvider.CreateAsyncScope();
            var dbContext = await InitAction(scope, context);
            if( dbContext == null ) return;

            var guild = await dbContext.Guilds
                .Include( g => g.AdminRoles )
                .Include( g => g.AdminUsers )
                .SingleOrDefaultAsync( g => g.GuildId == context.Guild.Id );
            if( guild == default )
            {
                await context.Interaction.FollowupAsync( $"No roles have been configured, effectively allowing **anyone** to admin me! Make sure to assign at least one admin role as soon as possible!", ephemeral: true );
                return;
            }

            var roleNames = guild.AdminRoles
                .Select( role => context.Guild.Roles.FirstOrDefault( r => r.Id == role.RoleId )?.Name )
                .Where( n => !string.IsNullOrEmpty( n ) )
                .Select( n => $"@{n}" )
                .ToList();
            if( !roleNames.Any() )
            {
                await context.Interaction.FollowupAsync( $"No roles have been configured, effectively allowing **anyone** to admin me! Make sure to assign at least one admin role as soon as possible!", ephemeral: true ); 
                return;
            }

            await context.Interaction.FollowupAsync( $"The following roles can admin me: {roleNames.Aggregate( ( f, s ) => $"{f}, {s}" )}", ephemeral: true  );
        }
        catch( Exception e )
        {
            _logger.LogError(e.Message);
            await context.Interaction.RespondAsync( $"Something went wrong. Please contact the my developers and let them know what happened.", ephemeral: true );
        }
    }

    public async Task AddEndpointAsync( IInteractionContext context)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = await InitAction(scope, context);
        if( dbContext == null ) return;
        
        var mb = new ModalBuilder()
            .WithTitle( "Add custom REST endpoint" )
            .WithCustomId( "custom-endpoint" )
            .AddTextInput( "Chain Name", "chain-name", TextInputStyle.Short, "myTestnet", 6, 32, true )
            .AddTextInput("REST Endpoint", "rest-endpoint", TextInputStyle.Short, "https://some-testnet.xyz", 16, 256, true)
            .AddTextInput( "REST Endpoint Provider", "provider-name", TextInputStyle.Short, "Brochain", 3, 32, true );
        await context.Interaction.RespondWithModalAsync( mb.Build() );
    }

    public async Task RemoveEndpointAsync( IInteractionContext context, string chainName, string providerName )
    {
        await context.Interaction.RespondAsync( "Verifying...", ephemeral: true );
        
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = await InitAction(scope, context);
        if( dbContext == null ) return;
        
        var chain = await dbContext.Chains
            .Include( c => c.Endpoints )
            .FirstOrDefaultAsync( c => c.Name == chainName );
        
        if( chain == null )
        {
            await context.Interaction.FollowupAsync( $"There is no chain registered by name {chainName}. Please check your input and try again", ephemeral: true );
            return;
        }

        var existingEndpointByProvider = chain.Endpoints.FirstOrDefault( e => e.Provider == providerName );
        if( existingEndpointByProvider == null )
        {
            await context.Interaction.FollowupAsync( $"There is no endpoint registered under provider name {providerName}", ephemeral: true );
            return;
        }

        chain.Endpoints.Remove( existingEndpointByProvider );
        dbContext.Endpoints.Remove( existingEndpointByProvider );
        await dbContext.SaveChangesAsync();
        
        await context.Interaction.FollowupAsync( $"Endpoint for provider {providerName} on {chainName} removed!", ephemeral: false);
    }

    public async Task AddCustomChainAsync( IInteractionContext context)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = await InitAction(scope, context);
        if( dbContext == null ) return;
        
        var mb = new ModalBuilder()
            .WithTitle( "Track a custom chain" )
            .WithCustomId( "custom-chain" )
            .AddTextInput( "Chain Name", "chain-name", TextInputStyle.Short, "myTestnet", 6, 32, true )
            .AddTextInput("REST Endpoint", "rest-endpoint", TextInputStyle.Short, "https://some-testnet.xyz", 16, 256, true)
            .AddTextInput( "REST Endpoint Provider", "provider-name", TextInputStyle.Short, "Brochain", 3, 32, true )
            .AddTextInput("Governance url", "gov-url", TextInputStyle.Short, "https://www.mintscan.io/cosmos/proposals", 16, 256, false)
            .AddTextInput("Image URL", "image-url", TextInputStyle.Short, "https://some-website.xyz/logo.png", 16, 256, false);
        await context.Interaction.RespondWithModalAsync( mb.Build() );
    }

    public async Task RemoveCustomChainAsync( IInteractionContext context, string chainName )
    {
        await context.Interaction.DeferAsync();
        
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = await InitAction(scope, context);
        if( dbContext == null ) return;
        
        var existingChain = await dbContext.Chains.FirstOrDefaultAsync( c => c.Name == chainName &&
                                                                             c.CustomForGuildId == context.Guild.Id );
        if( existingChain == default )
        {
            await context.Interaction.FollowupAsync( $"No custom chain by name '{chainName}' could be found for this server.", ephemeral: true );
            return;
        }

        dbContext.Remove( existingChain );
        await dbContext.SaveChangesAsync();

        await context.Interaction.FollowupAsync( $"Successfully removed tracking of custom chain '{chainName}'" );
    }
}
