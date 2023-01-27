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
            .AllowRole( Context, role );

    [SlashCommand( "revoke-admin-role", "Allows members of a given role to admin this bot" )]
    public async Task RevokeRole( IRole role )
        => await _serviceProvider.CreateScope().ServiceProvider
            .GetRequiredService<IConfigModuleActions>()
            .RevokeRole( Context, role );


    [SlashCommand( "list-admins", "List all admins (roles and users) for this bot" )]
    public async Task ListRoles()
        => await _serviceProvider.CreateScope().ServiceProvider
            .GetRequiredService<IConfigModuleActions>()
            .ListRoles( Context );

    [SlashCommand( "add-endpoint", "Add a REST endpoint for a chain" )]
    public async Task AddEndpoint()
        => await _serviceProvider.CreateScope().ServiceProvider
            .GetRequiredService<IConfigModuleActions>()
            .AddEndpoint( Context );

    [SlashCommand( "remove-endpoint", "Remove a REST endpoint for a chain" )]
    public async Task RemoveEndpoint( string chainName, string providerName )
        => await _serviceProvider.CreateScope().ServiceProvider
            .GetRequiredService<IConfigModuleActions>()
            .RemoveEndpoint( Context, chainName, providerName );

    [SlashCommand( "add-custom-chain", "Start tracking a custom chain (such as a testnet)" )]
    public async Task AddCustomChain()
        => await _serviceProvider.CreateScope().ServiceProvider
            .GetRequiredService<IConfigModuleActions>()
            .AddCustomChain( Context );

    [SlashCommand( "remove-custom-chain", "Remove tracking of a custom chain (such as a testnet)" )]
    public async Task RemoveCustomChain( string chainName )
        => await _serviceProvider.CreateScope().ServiceProvider
            .GetRequiredService<IConfigModuleActions>()
            .RemoveCustomChain( Context, chainName );
}
