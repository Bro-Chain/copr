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
    private readonly IServiceProvider _serviceProvider;

    public ConfigModule( ILogger<ConfigModule> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    [SlashCommand( "assign-admin-role", "Allows members of a given role to admin this bot" )]
    public async Task AllowRole( IRole role )
        => await _serviceProvider.CreateScope().ServiceProvider
            .GetRequiredService<IConfigModuleActions>()
            .AllowRoleAsync( Context, role );

    [SlashCommand( "revoke-admin-role", "Allows members of a given role to admin this bot" )]
    public async Task RevokeRole( IRole role )
        => await _serviceProvider.CreateScope().ServiceProvider
            .GetRequiredService<IConfigModuleActions>()
            .RevokeRoleAsync( Context, role );


    [SlashCommand( "list-admins", "List all admins (roles and users) for this bot" )]
    public async Task ListRoles()
        => await _serviceProvider.CreateScope().ServiceProvider
            .GetRequiredService<IConfigModuleActions>()
            .ListRolesAsync( Context );

    [SlashCommand( "add-endpoint", "Add a REST endpoint for a chain" )]
    public async Task AddEndpoint()
        => await _serviceProvider.CreateScope().ServiceProvider
            .GetRequiredService<IConfigModuleActions>()
            .AddEndpointAsync( Context );

    [SlashCommand( "remove-endpoint", "Remove a REST endpoint for a chain" )]
    public async Task RemoveEndpoint( string chainName, string providerName )
        => await _serviceProvider.CreateScope().ServiceProvider
            .GetRequiredService<IConfigModuleActions>()
            .RemoveEndpointAsync( Context, chainName, providerName );

    [SlashCommand( "add-custom-chain", "Start tracking a custom chain (such as a testnet)" )]
    public async Task AddCustomChain()
        => await _serviceProvider.CreateScope().ServiceProvider
            .GetRequiredService<IConfigModuleActions>()
            .AddCustomChainAsync( Context );

    [SlashCommand( "remove-custom-chain", "Remove tracking of a custom chain (such as a testnet)" )]
    public async Task RemoveCustomChain( string chainName )
        => await _serviceProvider.CreateScope().ServiceProvider
            .GetRequiredService<IConfigModuleActions>()
            .RemoveCustomChainAsync( Context, chainName );
}
