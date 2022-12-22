using System.Data.Common;
using System.Text.RegularExpressions;
using CosmosProposalBot.Configuration;
using CosmosProposalBot.Data;
using CosmosProposalBot.Data.Model;
using CosmosProposalBot.Util;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace CosmosProposalBot.Modules;

public class ButtonHandler
{
    private readonly ILogger<ButtonHandler> _logger;
    private readonly IOptions<BotOptions> _options;
    private readonly IServiceProvider _serviceProvider;

    private readonly Regex _chainNameRegex = new (@"^[A-Za-z][\w\d\-]{5,31}$");
    
    public ButtonHandler( ILogger<ButtonHandler> logger,
        IOptions<BotOptions> options,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _options = options;
        _serviceProvider = serviceProvider;
    }

    public async Task HandleModalAsync( SocketInteractionContext ctx, SocketMessageComponent component )
    {
        switch( component.Data.CustomId )
        {
            case "quick-subscribe-channel":
                await HandleSubscribeChannel( ctx, component );
                break;
            case "quick-subscribe-dm":
                await HandleSubscribeDm( ctx, component );
                break;
            case "quick-unsub-dm":
                await HandleUnsubscribeDm( ctx, component );
                break;
            default:
                await component.RespondAsync( "I don't know that command..." );
                break;
        }
    }

    private async Task<(bool, string)> TryGetChainName( SocketInteractionContext ctx, SocketMessageComponent component, string fieldName = "Chain name" )
    {
        var embed = component.Message.Embeds.FirstOrDefault();
        if( embed == default )
        {
            await component.FollowupAsync( "Something went wrong, and I can't match this button to a custom chain. Please use the `/subscribe channel` command instead" );
            return (false, string.Empty);
        }

        var chainNameField = embed.Fields.FirstOrDefault( f => f.Name == fieldName );
        if( chainNameField == default )
        {
            await component.FollowupAsync( "Something went wrong, and I can't match this button to a custom chain. Please use the `/subscribe channel` command instead" );
            return (false, string.Empty);
        }

        return ( true, chainNameField.Value );
    }
    
    private async Task HandleSubscribeChannel( SocketInteractionContext ctx, SocketMessageComponent component )
    {
        await component.RespondAsync( "Verifying...", ephemeral: true );

        var (success, chainName) = await TryGetChainName( ctx, component );
        if( !success )
        {
            return;
        }
            
        await using var scope = _serviceProvider.CreateAsyncScope();
        var subscriptionHelper = scope.ServiceProvider.GetRequiredService<SubscriptionHelper>();
        await subscriptionHelper.SubscribeChannel( ctx, chainName );
    }
    
    private async Task HandleSubscribeDm( SocketInteractionContext ctx, SocketMessageComponent component )
    {
        await component.RespondAsync( "Verifying...", ephemeral: true );

        var (success, chainName) = await TryGetChainName( ctx, component );
        if( !success )
        {
            return;
        }
        
        await using var scope = _serviceProvider.CreateAsyncScope();
        var subscriptionHelper = scope.ServiceProvider.GetRequiredService<SubscriptionHelper>();
        await subscriptionHelper.SubscribeDm( ctx, chainName );
    }
    
    private async Task HandleUnsubscribeDm( SocketInteractionContext ctx, SocketMessageComponent component )
    {
        await component.DeferAsync();

        var (success, chainName) = await TryGetChainName( ctx, component );
        if( !success )
        {
            return;
        }
        
        await using var scope = _serviceProvider.CreateAsyncScope();
        var subscriptionHelper = scope.ServiceProvider.GetRequiredService<SubscriptionHelper>();
        await subscriptionHelper.UnsubscribeDm( ctx, chainName );
    }
}
