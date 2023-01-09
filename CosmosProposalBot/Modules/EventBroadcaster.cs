using System.Reflection.Emit;
using System.Text;
using CosmosProposalBot.Data;
using CosmosProposalBot.Data.Model;
using CosmosProposalBot.Model;
using CosmosProposalBot.Util;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CosmosProposalBot.Modules;

public interface IEventBroadcaster
{
    Task BroadcastStatusChangeAsync( Proposal prop, string newStatus );
    Task BroadcastNewUpgradeAsync( Proposal prop, string newStatus, ProposalInfoUpgradePlan plan );
    Task BroadcastUpgradeReminderAsync( TrackedEvent trackedEvent );
}

public class EventBroadcaster : IEventBroadcaster
{
    private readonly ILogger<EventBroadcaster> _logger;
    private readonly IServiceProvider _serviceProvider;

    public EventBroadcaster( 
        ILogger<EventBroadcaster> logger,
        IServiceProvider serviceProvider )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }
    
    public async Task BroadcastStatusChangeAsync( Proposal prop, string newStatus )
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CopsDbContext>();
        var socketClient = scope.ServiceProvider.GetRequiredService<DiscordSocketClient>();
        
        var dmSubscribers = await dbContext.UserSubscriptions
            .Include( s => s.Chain )
            .Where( s => s.Chain.Id == prop.Chain.Id )
            .ToListAsync();

        var channelSubscriptions = await dbContext.ChannelSubscriptions
            .Include( s => s.Chain )
            .Where( s => s.Chain.Id == prop.Chain.Id )
            .ToListAsync();
        
        foreach (var sub in dmSubscribers)
        {
            try
            {
                var user = await socketClient.GetUserAsync( sub.DiscordUserId );
                if( user == default )
                {
                    _logger.LogError("Could not find user with id {UserId} for subscription to proposals on {Chain}", sub.DiscordUserId, sub.Chain.Name );
                    continue;
                }

                var dmChannel = await user.CreateDMChannelAsync();

                _logger.LogInformation("{ServiceName} broadcasting proposal info to {User}#{Discriminator}", nameof(EventBroadcaster), user.Username, user.Discriminator);
                
                var cb = new ComponentBuilder()
                    .WithButton("Unsubscribe", "quick-unsub-dm", ButtonStyle.Danger, new Emoji("🚫"));

                await dmChannel.SendMessageAsync("", embed: await GenerateProposalEmbed( prop, newStatus ), components: cb.Build());
            }
            catch( Exception e )
            {
                _logger.LogError($"Failed to send DM: {e}");
            }
        }

        foreach( var sub in channelSubscriptions )
        {
            try
            {
                var guild = socketClient.GetGuild( sub.GuildId );
                if( guild == default )
                {
                    _logger.LogError("Could not find guild with id {GuildId} for subscription to proposals on {Chain}", sub.GuildId, sub.Chain.Name );
                    continue;
                }
                
                var channel = guild.GetChannel( sub.DiscordChannelId ) as IMessageChannel;
                if( channel == default )
                {
                    _logger.LogError("Could not find channel with id {ChannelId} for subscription to proposals on {Chain}", sub.DiscordChannelId, sub.Chain.Name );
                    continue;
                }
                
                _logger.LogInformation("{ServiceName} broadcasting proposal info to {GuildName} : {ChannelName}", nameof(EventBroadcaster), guild.Name, channel.Name);
                await channel.SendMessageAsync("", embed: await GenerateProposalEmbed( prop, newStatus ));
            }
            catch( Exception e )
            {
                _logger.LogError($"Failed to send channel message: {e}");
            }
        }
    }

    private async Task<Embed> GenerateProposalEmbed( Proposal prop, string newStatus )
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var imageFetcher = scope.ServiceProvider.GetRequiredService<IImageFetcher>();
        
        var title = ( prop.Status, newStatus ) switch
        {
            (_,Constants.ProposalStatusPassed) => "✅ PASSED",
            (_,Constants.ProposalStatusRejected) => "❌ REJECTED",
            (_,Constants.ProposalStatusVotingPeriod) => "🕑 VOTING on",
            (null,_) => "New",
            _ => ""
        };

        var color = ( prop.Status, newStatus ) switch
        {
            (_,Constants.ProposalStatusPassed) => Color.Green,
            (_,Constants.ProposalStatusRejected) => Color.Red,
            (_,Constants.ProposalStatusVotingPeriod) => Color.Blue,
            _ => Color.Default
        };
        
        var fields = new List<EmbedFieldBuilder>
        {
            new EmbedFieldBuilder()
                .WithName( "Tracking Name" )
                .WithValue( prop.Chain.Name )
                .WithIsInline(true),
            new EmbedFieldBuilder()
                .WithName( "Chain Id" )
                .WithValue( prop.Chain.ChainId )
                .WithIsInline(true),
            new EmbedFieldBuilder()
                .WithName( "Description" )
                .WithValue( TrimFieldValue(prop.Description) ),
            new EmbedFieldBuilder()
                .WithName( "Proposal Type" )
                .WithValue( prop.ProposalType )
                .WithIsInline(true),
            new EmbedFieldBuilder()
                .WithName( "Status" )
                .WithValue( $"{newStatus}" )
                .WithIsInline(true)
        };
        if( !string.IsNullOrEmpty( prop.Link ) )
        {
            fields.Add(new EmbedFieldBuilder()
                .WithName( "Explorer URL" )
                .WithValue( prop.Link ));
        }
        
        var eb = new EmbedBuilder()
            .WithTitle( $"{title} governance proposal #{prop.ProposalId}" )
            .WithFields( fields )
            .WithFooter( "CØPR - CØsmos PRoposal bot - by Brochain" )
            .WithColor( color );

        if(!string.IsNullOrEmpty(prop.Chain.ImageUrl))
        {
            eb.WithThumbnailUrl( await imageFetcher.FetchImage( prop.Chain.ImageUrl, prop.Chain.Name ) );
        }

        return eb.Build();
    }

    public async Task BroadcastNewUpgradeAsync( Proposal prop, string newStatus, ProposalInfoUpgradePlan plan )
    {
        if( prop.ProposalType != Constants.ProposalTypeSoftwareUpgrade )
        {
            return;
        }
        if( prop.Status == Constants.ProposalStatusVotingPeriod && newStatus == Constants.ProposalStatusPassed )
        {
            // assume we already knew about this upgrade as the status has previously been recorded
            // possible bug when the bot is down for a while
            return;
        }
        if( newStatus is not Constants.ProposalStatusVotingPeriod and not Constants.ProposalStatusPassed )
        {
            return;
        }
        // TODO: handle rejected upgrades
        
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CopsDbContext>();
        var socketClient = scope.ServiceProvider.GetRequiredService<DiscordSocketClient>();

        var localProp = await dbContext.Proposals
            .Include( p => p.Chain )
            .FirstOrDefaultAsync( p => p.Id == prop.Id );
        var channelSubscriptions = await dbContext.ChannelSubscriptions
            .Include( s => s.Chain )
            .Where( s => s.Chain.Id == prop.Chain.Id )
            .ToListAsync();

        var trackedEvent = new TrackedEvent
        {
            Proposal = localProp,
            Height = ulong.Parse( plan.Height )
        };

        await dbContext.TrackedEvents.AddAsync( trackedEvent );
        
        foreach( var sub in channelSubscriptions )
        {
            try
            {
                var guild = socketClient.GetGuild( sub.GuildId );
                if( guild == default )
                {
                    _logger.LogError("Could not find guild with id {GuildId} for subscription to proposals on {Chain}", sub.GuildId, sub.Chain.Name );
                    continue;
                }
                
                var channel = guild.GetChannel( sub.DiscordChannelId ) as ITextChannel;
                if( channel == default )
                {
                    _logger.LogError("Could not find channel with id {ChannelId} for subscription to proposals on {Chain}", sub.DiscordChannelId, sub.Chain.Name );
                    continue;
                }
                
                _logger.LogInformation("{ServiceName} broadcasting upgrade info to {GuildName} : {ChannelName}", nameof(EventBroadcaster), guild.Name, channel.Name);

                var threadChannel = await channel.CreateThreadAsync( $"Upgrade {localProp.Chain.Name}: {plan.Name}" );
                var eventThread = new TrackedEventThread
                {
                    ThreadId = threadChannel.Id,
                    GuildId = guild.Id,
                    TrackedEvent = trackedEvent
                };
                await dbContext.AddAsync( eventThread );
                trackedEvent.Threads.Add(eventThread);

                await threadChannel.SendMessageAsync("New upgrade incoming!", embed: await GenerateUpgradeEmbed( localProp, plan ));
            }
            catch( Exception e )
            {
                _logger.LogError($"Failed to send channel message: {e}");
            }
        }
        await dbContext.SaveChangesAsync();
    }

    private async Task<Embed> GenerateUpgradeEmbed( Proposal prop, ProposalInfoUpgradePlan plan )
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var imageFetcher = scope.ServiceProvider.GetRequiredService<IImageFetcher>();
        
        var fields = new List<EmbedFieldBuilder>
        {
            new EmbedFieldBuilder()
                .WithName( "Chain Name" )
                .WithValue( prop.Chain.Name ),
            new EmbedFieldBuilder()
                .WithName( "Upgrade alias" )
                .WithValue( plan.Name )
                .WithIsInline( true ),
            new EmbedFieldBuilder()
                .WithName( "Upgrade height" )
                .WithValue( plan.Height )
                .WithIsInline( true )
        };
        if( !string.IsNullOrEmpty( prop.Link ) )
        {
            fields.Add(new EmbedFieldBuilder()
                .WithName( "Explorer URL" )
                .WithValue( prop.Link ));
        }
        
        var eb = new EmbedBuilder()
            .WithTitle( $"Upgrade from proposal #{prop.ProposalId}" )
            .WithFields( fields )
            .WithFooter( "CØPR - CØsmos PRoposal bot - by Brochain" )
            .WithColor( Color.Green );

        if(!string.IsNullOrEmpty(prop.Chain.ImageUrl))
        {
            eb.WithThumbnailUrl( await imageFetcher.FetchImage( prop.Chain.ImageUrl, prop.Chain.Name ) );
        }

        return eb.Build();
    }

    public async Task BroadcastUpgradeReminderAsync( TrackedEvent trackedEvent )
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var socketClient = scope.ServiceProvider.GetRequiredService<DiscordSocketClient>();
        
        foreach( var thread in trackedEvent.Threads )
        {
            var guild = socketClient.GetGuild( thread.GuildId );
            if( guild == default )
            {
                _logger.LogError("Could not find guild with id {GuildId} for subscription to proposals on {Chain}", thread.GuildId, trackedEvent.Proposal.Chain.Name );
                continue;
            }
                
            var channel = guild.GetChannel( thread.ThreadId ) as ITextChannel;
            if( channel == default )
            {
                _logger.LogError("Could not find channel with id {ChannelId} for subscription to proposals on {Chain}", thread.ThreadId, trackedEvent.Proposal.Chain.Name );
                continue;
            }
            
            await channel.SendMessageAsync("", embed: await GenerateUpgradeUpdateEmbed( trackedEvent ));
        }
    }

    private async Task<Embed> GenerateUpgradeUpdateEmbed( TrackedEvent trackedEvent )
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var imageFetcher = scope.ServiceProvider.GetRequiredService<IImageFetcher>();
        
        var fields = new List<EmbedFieldBuilder>
        {
            new EmbedFieldBuilder()
                .WithName( "Chain Name" )
                .WithValue( trackedEvent.Proposal.Chain.Name ),
            new EmbedFieldBuilder()
                .WithName( "Upgrade height" )
                .WithValue( trackedEvent.Height ),
            new EmbedFieldBuilder()
                .WithName( "Estimated upgrade time" )
                .WithValue( $"{trackedEvent.HeightEstimatedAt:yyyy-MM-dd HH:mm:ss} UTC" )
                .WithIsInline(true),
            new EmbedFieldBuilder()
                .WithName( "Time left" )
                .WithValue( $"~ {GetRoughTimeLeft( trackedEvent.HeightEstimatedAt - DateTime.UtcNow )}" )
                .WithIsInline(true)
        };
        
        var eb = new EmbedBuilder()
            .WithTitle( $"🚨 🚨 🚨 Upgrade reminder for {trackedEvent.Proposal.Chain.Name} 🚨 🚨 🚨" )
            .WithFields( fields )
            .WithFooter( "CØPR - CØsmos PRoposal bot - by Brochain" )
            .WithColor( Color.Green );

        if(!string.IsNullOrEmpty(trackedEvent.Proposal.Chain.ImageUrl))
        {
            eb.WithThumbnailUrl( await imageFetcher.FetchImage( trackedEvent.Proposal.Chain.ImageUrl, trackedEvent.Proposal.Chain.Name ) );
        }

        return eb.Build();
    }

    private string GetRoughTimeLeft( TimeSpan? trackedEventHeightEstimatedAt )
    { 
        if( trackedEventHeightEstimatedAt == null )
        {
            return "Unknown";
        }
        var timeLeft = trackedEventHeightEstimatedAt.Value;
        if( timeLeft.TotalHours > 24 )
        {
            return $"{timeLeft.Days} day{(timeLeft.Days > 1 ? "s" : "")} {timeLeft.Hours} hour{(timeLeft.Hours > 1 ? "s" : "")}";
        }
        if( timeLeft.TotalMinutes > 60 )
        {
            return $"{timeLeft.Hours} hour{(timeLeft.Hours > 1 ? "s" : "")}";
        }
        return $"{timeLeft.Minutes} minute{(timeLeft.Minutes > 1 ? "s" : "")}";
    }

    private static string TrimFieldValue( string value )
    {
        var modifiedValue = value
            .Replace( "\\n", "\n" );
        
        if( modifiedValue.Length > 1024 )
        {
            return modifiedValue[..1015] + "\n\n[...]";
        }

        return modifiedValue;
    }
}
