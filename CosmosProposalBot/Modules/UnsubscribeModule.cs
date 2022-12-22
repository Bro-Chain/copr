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

[Discord.Interactions.Group("unsubscribe","Unsubscribe from notifications")]
public class UnsubscribeModule : InteractionModuleBase
{
    private readonly ILogger<UnsubscribeModule> _logger;
    private readonly IOptions<BotOptions> _options;
    private readonly IServiceProvider _serviceProvider;

    public UnsubscribeModule( ILogger<UnsubscribeModule> logger,
        IOptions<BotOptions> options,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _options = options;
        _serviceProvider = serviceProvider;
    }
    
    [SlashCommand( "private", "Subscribes to DMs about proposals for a given chain" )]
    public async Task UnsubscribePrivate( string chainName )
    {
        try
        {
            await DeferAsync( ephemeral: true );

            await using var scope = _serviceProvider.CreateAsyncScope();
            var subscriptionHelper = scope.ServiceProvider.GetRequiredService<SubscriptionHelper>();
            
            await subscriptionHelper.UnsubscribeDm( Context, chainName );
        }
        catch( Exception e )
        {
            Console.WriteLine( e );
            throw;
        }
    }

    [SlashCommand( "channel", "Subscribes to channel notifications about proposals for a given chain" )]
    public async Task UnsubscribeChannel( string chainName )
    {
        try
        {
            await DeferAsync();

            await using var scope = _serviceProvider.CreateAsyncScope();
            var subscriptionHelper = scope.ServiceProvider.GetRequiredService<SubscriptionHelper>();
            
            await subscriptionHelper.UnsubscribeChannel( Context, chainName );
        }
        catch( Exception e )
        {
            Console.WriteLine( e );
            throw;
        }
    }
}
