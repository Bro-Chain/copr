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
    {
        try
        {
            await DeferAsync();
            
            await using var scope = _serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CopsDbContext>();
            var guildSpecificChains = dbContext.Chains
                .Where( c => c.CustomForGuildId == Context.Guild.Id )
                .Select( c => $"`{c.Name}`" )
                .ToList();

            var standardChains = _options.Value.SupportedChains
                .Select( c => $"`{c}`" );

            var eb = new EmbedBuilder()
                .WithTitle( "Supported Chains" )
                .WithFields(
                    new EmbedFieldBuilder()
                        .WithName("Standard")
                        .WithValue(string.Join(", ", standardChains)),
                    new EmbedFieldBuilder()
                        .WithName("Custom")
                        .WithValue(string.Join(", ", guildSpecificChains)));

            await FollowupAsync($"", embed: eb.Build());
        }
        catch( Exception e )
        {
            Console.WriteLine( e );
            throw;
        }
    }

    [SlashCommand( "private", "Subscribes to DMs about proposals for a given chain" )]
    public async Task SubscribePrivate( string chainName )
    {
        try
        {
            await DeferAsync( ephemeral: true );

            await using var scope = _serviceProvider.CreateAsyncScope();
            var subscriptionHelper = scope.ServiceProvider.GetRequiredService<SubscriptionHelper>();
            
            await subscriptionHelper.SubscribeDm( Context, chainName );
        }
        catch( Exception e )
        {
            Console.WriteLine( e );
            throw;
        }
    }

    [SlashCommand( "channel", "Subscribes to channel notifications about proposals for a given chain" )]
    public async Task SubscribeChannel( string chainName )
    {
        try
        {
            await DeferAsync();

            await using var scope = _serviceProvider.CreateAsyncScope();
            var subscriptionHelper = scope.ServiceProvider.GetRequiredService<SubscriptionHelper>();
            
            await subscriptionHelper.SubscribeChannel( Context, chainName );
        }
        catch( Exception e )
        {
            Console.WriteLine( e );
            throw;
        }
    }
}
