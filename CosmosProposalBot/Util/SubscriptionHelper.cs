using CosmosProposalBot.Data;
using CosmosProposalBot.Data.Model;
using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CosmosProposalBot.Util;

public class SubscriptionHelper
{
    private readonly ILogger<SubscriptionHelper> _logger;
    private readonly IServiceProvider _serviceProvider;

    public SubscriptionHelper( ILogger<SubscriptionHelper> logger, IServiceProvider serviceProvider )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task SubscribeChannel( IInteractionContext context, string chainName )
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CopsDbContext>();
        var permissionHelper = scope.ServiceProvider.GetRequiredService<PermissionHelper>();
        
        if( !await permissionHelper.EnsureUserHasPermission( context, dbContext ) )
        {
            await context.Interaction.FollowupAsync( "You do not have permission to use this command", ephemeral: true );
            return;
        }
            
        var existingSubscription = await dbContext.ChannelSubscriptions
            .Include( us => us.Chain )
            .FirstOrDefaultAsync( u => u.DiscordChannelId == context.Channel.Id &&
                                       u.Chain.Name.ToLower() == chainName.ToLower()&&
                                       (u.Chain.CustomForGuildId == null || u.Chain.CustomForGuildId == context.Guild.Id)  );
        if(existingSubscription != null)
        {
            await context.Interaction.FollowupAsync( $"Channel is **already** subscribed to proposals for `{chainName}`" );
            return;
        }

        var chain = await dbContext.Chains.FirstOrDefaultAsync( c => c.Name.ToLower() == chainName.ToLower() );
        if( chain == default )
        {
            await context.Interaction.FollowupAsync($"Chain `{chainName}` is not supported");
            return;
        }

        dbContext.ChannelSubscriptions.Add( new ChannelSubscription
        {
            Chain = chain,
            GuildId = context.Guild.Id,
            DiscordChannelId = context.Channel.Id
        } );
        await dbContext.SaveChangesAsync();
            
        await context.Interaction.FollowupAsync($"**This channel** is now **subscribed** to proposal updates for `{chainName}`");
    }

    public async Task SubscribeDm( IInteractionContext context, string chainName )
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CopsDbContext>();
            
        var existingSubscription = await dbContext.UserSubscriptions
            .Include( us => us.Chain )
            .FirstOrDefaultAsync( u => u.DiscordUserId == context.User.Id &&
                                       u.Chain.Name.ToLower() == chainName.ToLower() &&
                                       (u.Chain.CustomForGuildId == null || u.Chain.CustomForGuildId == context.Guild.Id) );

        if(existingSubscription != null)
        {
            await context.Interaction.FollowupAsync( $"You are **already** subscribed to proposals for {chainName} in DMs", ephemeral:true );
            return;
        }

        var chain = await dbContext.Chains.FirstOrDefaultAsync( c => c.Name.ToLower() == chainName.ToLower() );
        if( chain == default )
        {
            await context.Interaction.FollowupAsync($"Chain `{chainName}` is not supported", ephemeral:true);
            return;
        }

        if( chain.CustomForGuildId != null )
        {
            await context.Interaction.FollowupAsync($"Chain `{chainName}` is a custom chain and is not supported for DM subscriptions", ephemeral:true);
            return;
        }

        dbContext.UserSubscriptions.Add( new UserSubscription
        {
            Chain = chain,
            DiscordUserId = context.User.Id,
            Username = context.User.Username,
            Discriminator = context.User.Discriminator
        } );
        await dbContext.SaveChangesAsync();
        
        await context.Interaction.FollowupAsync($"You are now **subscribed** to proposal updates for `{chainName}` in DMs", ephemeral:true);
    }

    public async Task UnsubscribeChannel( IInteractionContext context, string chainName )
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CopsDbContext>();
        var permissionHelper = scope.ServiceProvider.GetRequiredService<PermissionHelper>();

        if( !await permissionHelper.EnsureUserHasPermission( context, dbContext ) )
        {
            await context.Interaction.FollowupAsync( "You do not have permission to use this command", ephemeral: true );
            return;
        }
            
        var existingSubscription = await dbContext.ChannelSubscriptions
            .Include( us => us.Chain )
            .FirstOrDefaultAsync( u => u.DiscordChannelId == context.Channel.Id &&
                                       u.Chain.Name.ToLower() == chainName.ToLower() &&
                                       (u.Chain.CustomForGuildId == null || u.Chain.CustomForGuildId == context.Guild.Id)  );
        if(existingSubscription == null)
        {
            await context.Interaction.FollowupAsync( $"Channel is **not** subscribed to proposals for `{chainName}`" );
            return;
        }

        dbContext.ChannelSubscriptions.Remove( existingSubscription );
        await dbContext.SaveChangesAsync();
            
        await context.Interaction.FollowupAsync($"**This channel** is now **unsubscribed** to proposal updates for `{chainName}`");
    }

    public async Task UnsubscribeDm( IInteractionContext context, string chainName )
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CopsDbContext>();
            
        var existingSubscription = await dbContext.UserSubscriptions
            .Include( us => us.Chain )
            .FirstOrDefaultAsync( u => u.DiscordUserId == context.User.Id &&
                                       u.Chain.Name.ToLower() == chainName.ToLower() &&
                                       u.Chain.CustomForGuildId == null );
        if(existingSubscription == null)
        {
            await context.Interaction.FollowupAsync( $"You are **not** subscribed to proposals for `{chainName}` in DMs" , ephemeral: true);
            return;
        }

        var chain = await dbContext.Chains.FirstOrDefaultAsync( c => c.Name.ToLower() == chainName.ToLower() );
        if( chain == default )
        {
            await context.Interaction.FollowupAsync($"Chain `{chainName}` is not supported", ephemeral: true);
            return;
        }

        dbContext.UserSubscriptions.Remove( existingSubscription );
        await dbContext.SaveChangesAsync();
            
        await context.Interaction.FollowupAsync($"You are now **unsubscribed** to proposal updates for `{chainName}` in DMs", ephemeral: true);
    }
}
