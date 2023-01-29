using System.Text;
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

[Discord.Interactions.Group("subscribe","Subscribe to notifications")]
public class SubscribeModule : InteractionModuleBase
{
    private readonly ILogger<SubscribeModule> _logger;
    private readonly IOptions<BotOptions> _options;
    private readonly IServiceProvider _serviceProvider;

    public SubscribeModule( ILogger<SubscribeModule> logger,
        IOptions<BotOptions> options,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _options = options;
        _serviceProvider = serviceProvider;
    }

    [SlashCommand("supported-chains", "Lists all chains that the bot currently supports")]
    public async Task SupportedChains()
        => await _serviceProvider.CreateScope().ServiceProvider
            .GetRequiredService<ISubscribeModuleActions>()
            .SupportedChains( Context );

    [SlashCommand( "private", "Subscribes to DMs about proposals for a given chain" )]
    public async Task SubscribePrivate( string chainName )
        => await _serviceProvider.CreateScope().ServiceProvider
            .GetRequiredService<ISubscribeModuleActions>()
            .SubscribePrivate( Context, chainName );

    [SlashCommand( "channel", "Subscribes to channel notifications about proposals for a given chain" )]
    public async Task SubscribeChannel( string chainName )
        => await _serviceProvider.CreateScope().ServiceProvider
            .GetRequiredService<ISubscribeModuleActions>()
            .SubscribeChannel( Context, chainName );
}
