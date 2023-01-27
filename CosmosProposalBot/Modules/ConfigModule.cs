using CosmosProposalBot.Configuration;
using CosmosProposalBot.Data;
using CosmosProposalBot.Data.Model;
using CosmosProposalBot.Util;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CosmosProposalBot.Modules;

[Discord.Interactions.Group("config","Configure the bot")]
public class ConfigModule : InteractionModuleBase
{
    private readonly ILogger<ConfigModule> _logger;
    private readonly IOptions<BotOptions> _options;
    private readonly IServiceProvider _serviceProvider;

    public ConfigModule( ILogger<ConfigModule> logger,
        IOptions<BotOptions> options,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _options = options;
        _serviceProvider = serviceProvider;
    }

    [SlashCommand( "assign-admin-role", "Allows members of a given role to admin this bot" )]
    public async Task AllowRole( IRole role )
    {
        try
        {
            await DeferAsync( ephemeral: true );

            await using var scope = _serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CopsDbContext>();
            var permissionHelper = scope.ServiceProvider.GetRequiredService<IPermissionHelper>();

            if( !await permissionHelper.EnsureUserHasPermission( Context, dbContext ) )
            {
                await FollowupAsync( "You do not have permission to use this command", ephemeral: true );
                return;
            }

            var guild = await dbContext.Guilds
                .AsSplitQuery()
                .Include( g => g.AdminRoles )
                .SingleOrDefaultAsync( g => g.GuildId == Context.Guild.Id );
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
                await FollowupAsync( $"Role @{role.Name} was **added** as admin group", ephemeral: true );
            }
            else
            {
                await dbContext.SaveChangesAsync();
                await FollowupAsync( $"Role @{role.Name} is **already** an admin group", ephemeral: true );
            }
        }
        catch( Exception e )
        {
            _logger.LogError($"{e.Message}");
            await RespondAsync( $"Something went wrong. Please contact the my developers and let them know what happened.", ephemeral: true );
        }
    }

    [SlashCommand( "revoke-admin-role", "Allows members of a given role to admin this bot" )]
    public async Task RevokeRole( IRole role )
    {
        try
        {
            await DeferAsync( ephemeral: true );

            await using var scope = _serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CopsDbContext>();
            var permissionHelper = scope.ServiceProvider.GetRequiredService<IPermissionHelper>();

            if( !await permissionHelper.EnsureUserHasPermission( Context, dbContext ) )
            {
                await FollowupAsync( "You do not have permission to use this command", ephemeral: true );
                return;
            }

            var guild = await dbContext.Guilds
                .Include( g => g.AdminRoles )
                .SingleOrDefaultAsync( g => g.GuildId == role.Guild.Id );
            if( guild == default )
            {
                await FollowupAsync( $"Role @{role.Name} **is not** an admin group", ephemeral: true );
                return;
            }

            var existingRole = guild.AdminRoles.FirstOrDefault( r => r.RoleId == role.Id );
            if( existingRole == default )
            {
                await FollowupAsync( $"Role @{role.Name} **is not** an admin group", ephemeral: true );
                return;
            }

            guild.AdminRoles.RemoveAll( r => r.RoleId == role.Id );
            await FollowupAsync( $"Role @{role.Name} was **revoked** as admin group", ephemeral: true );
            
            await dbContext.SaveChangesAsync();
        }
        catch( Exception e )
        {
            _logger.LogError($"{e.Message}");
            await RespondAsync( $"Something went wrong. Please contact the my developers and let them know what happened.", ephemeral: true );
        }
    }

    [SlashCommand( "list-admins", "List all admins (roles and users) for this bot" )]
    public async Task ListRoles()
    {
        try
        {
            await DeferAsync( ephemeral: true );

            await using var scope = _serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CopsDbContext>();
            var permissionHelper = scope.ServiceProvider.GetRequiredService<IPermissionHelper>();

            if( !await permissionHelper.EnsureUserHasPermission( Context, dbContext ) )
            {
                await FollowupAsync( "You do not have permission to use this command", ephemeral: true );
                return;
            }

            var guild = await dbContext.Guilds
                .Include( g => g.AdminRoles )
                .Include( g => g.AdminUsers )
                .SingleOrDefaultAsync( g => g.GuildId == Context.Guild.Id );
            if( guild == default )
            {
                await FollowupAsync( $"No roles have been configured, effectively allowing **anyone** to admin me! Make sure to assign at least one admin role as soon as possible!", ephemeral: true );
                return;
            }

            var roleNames = guild.AdminRoles
                .Select( role => Context.Guild.Roles.FirstOrDefault( r => r.Id == role.RoleId )?.Name )
                .Where( n => !string.IsNullOrEmpty( n ) )
                .Select( n => $"@{n}" )
                .ToList();
            if( !roleNames.Any() )
            {
                await FollowupAsync( $"No roles have been configured, effectively allowing **anyone** to admin me! Make sure to assign at least one admin role as soon as possible!", ephemeral: true ); 
                return;
            }

            await FollowupAsync( $"The following roles can admin me: {roleNames.Aggregate( ( f, s ) => $"{f}, {s}" )}" );
        }
        catch( Exception e )
        {
            _logger.LogError($"{e.Message}");
            await RespondAsync( $"Something went wrong. Please contact the my developers and let them know what happened.", ephemeral: true );
        }
    }

    [SlashCommand( "add-endpoint", "Add a REST endpoint for a chain" )]
    public async Task AddEndpoint()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CopsDbContext>();
        var permissionHelper = scope.ServiceProvider.GetRequiredService<IPermissionHelper>();

        if( !await permissionHelper.EnsureUserHasPermission( Context, dbContext ) )
        {
            await FollowupAsync( "You do not have permission to use this command", ephemeral: true );
            return;
        }
        
        var mb = new ModalBuilder()
            .WithTitle( "Add custom REST endpoint" )
            .WithCustomId( "custom-endpoint" )
            .AddTextInput( "Chain Name", "chain-name", TextInputStyle.Short, "myTestnet", 6, 32, true )
            .AddTextInput("REST Endpoint", "rest-endpoint", TextInputStyle.Short, "https://some-testnet.xyz", 16, 256, true)
            .AddTextInput( "REST Endpoint Provider", "provider-name", TextInputStyle.Short, "Brochain", 3, 32, true );
        await RespondWithModalAsync( mb.Build() );
    }

    [SlashCommand( "remove-endpoint", "Remove a REST endpoint for a chain" )]
    public async Task RemoveEndpoint( string chainName, string providerName )
    {
        await RespondAsync( "Verifying...", ephemeral: true );

        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CopsDbContext>();
        
        var chain = await dbContext.Chains
            .Include( c => c.Endpoints )
            .FirstOrDefaultAsync( c => c.Name == chainName );
        
        if( chain == null )
        {
            await FollowupAsync( $"There is no chain registered by name {chainName}. Please check your input and try again", ephemeral: true );
            return;
        }

        var existingEndpointByProvider = chain.Endpoints.FirstOrDefault( e => e.Provider == providerName );
        if( existingEndpointByProvider == null )
        {
            await FollowupAsync( $"There is no endpoint registered under provider name {providerName}", ephemeral: true );
            return;
        }

        chain.Endpoints.Remove( existingEndpointByProvider );
        dbContext.Endpoints.Remove( existingEndpointByProvider );
        await dbContext.SaveChangesAsync();
        
        await FollowupAsync( $"Endpoint for provider {providerName} on {chainName} removed!", ephemeral: false);
    }

    [SlashCommand( "add-custom-chain", "Start tracking a custom chain (such as a testnet)" )]
    public async Task AddCustomChain()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CopsDbContext>();
        var permissionHelper = scope.ServiceProvider.GetRequiredService<IPermissionHelper>();

        if( !await permissionHelper.EnsureUserHasPermission( Context, dbContext ) )
        {
            await FollowupAsync( "You do not have permission to use this command", ephemeral: true );
            return;
        }
        
        var mb = new ModalBuilder()
            .WithTitle( "Track a custom chain" )
            .WithCustomId( "custom-chain" )
            .AddTextInput( "Chain Name", "chain-name", TextInputStyle.Short, "myTestnet", 6, 32, true )
            .AddTextInput("REST Endpoint", "rest-endpoint", TextInputStyle.Short, "https://some-testnet.xyz", 16, 256, true)
            .AddTextInput( "REST Endpoint Provider", "provider-name", TextInputStyle.Short, "Brochain", 3, 32, true )
            .AddTextInput("Governance url", "gov-url", TextInputStyle.Short, "https://www.mintscan.io/cosmos/proposals", 16, 256, false)
            .AddTextInput("Image URL", "image-url", TextInputStyle.Short, "https://some-website.xyz/logo.png", 16, 256, false);
        await RespondWithModalAsync( mb.Build() );
    }

    [SlashCommand( "remove-custom-chain", "Remove tracking of a custom chain (such as a testnet)" )]
    public async Task RemoveCustomChain( string chainName )
    {
        await DeferAsync();
        
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CopsDbContext>();
        var permissionHelper = scope.ServiceProvider.GetRequiredService<IPermissionHelper>();

        if( !await permissionHelper.EnsureUserHasPermission( Context, dbContext ) )
        {
            await FollowupAsync( "You do not have permission to use this command", ephemeral: true );
            return;
        }
        
        var existingChain = await dbContext.Chains.FirstOrDefaultAsync( c => c.Name == chainName &&
                                                                             c.CustomForGuildId == Context.Guild.Id );
        if( existingChain == default )
        {
            await FollowupAsync( $"No custom chain by name '{chainName}' could be found for this server.", ephemeral: true );
            return;
        }

        dbContext.Remove( existingChain );
        await dbContext.SaveChangesAsync();

        await FollowupAsync( $"Successfully removed tracking of custom chain '{chainName}'" );
    }
}
