using System.Reflection.Emit;
using System.Text;
using CosmosProposalBot.Data;
using CosmosProposalBot.Data.Model;
using CosmosProposalBot.Model;
using CosmosProposalBot.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Logging;

namespace CosmosProposalBot.Modules;

public class EventBroadcaster
{
    private readonly ILogger<EventBroadcaster> _logger;
    private readonly DiscordSocketClient _socketClient;
    private readonly ImageFetcher _imageFetcher;
    private readonly CopsDbContext _dbContext;

    public EventBroadcaster( 
        ILogger<EventBroadcaster> logger,
        DiscordSocketClient socketClient,
        ImageFetcher imageFetcher,
        CopsDbContext dbContext )
    {
        _logger = logger;
        _socketClient = socketClient;
        _imageFetcher = imageFetcher;
        _dbContext = dbContext;
    }
    
    public async Task BroadcastStatusChangeAsync( Proposal prop, string newStatus )
    {
        var dmSubscribers = await _dbContext.UserSubscriptions
            .Include( s => s.Chain )
            .Where( s => s.Chain.Id == prop.Chain.Id )
            .ToListAsync();

        var channelSubscriptions = await _dbContext.ChannelSubscriptions
            .Include( s => s.Chain )
            .Where( s => s.Chain.Id == prop.Chain.Id )
            .ToListAsync();
        
        foreach (var sub in dmSubscribers)
        {
            try
            {
                var user = await _socketClient.GetUserAsync( sub.DiscordUserId );
                if( user == default )
                {
                    _logger.LogError("Could not find user with id {UserId} for subscription to proposals on {Chain}", sub.DiscordUserId, sub.Chain.Name );
                    continue;
                }

                var dmChannel = await user.CreateDMChannelAsync();

                _logger.LogInformation("{ServiceName} broadcasting proposal info to {User}#{Discriminator}", nameof(EventBroadcaster), user.Username, user.Discriminator);
                
                var cb = new ComponentBuilder()
                    .WithButton("Unsubscribe", "quick-unsub-dm", ButtonStyle.Danger, new Emoji("🚫"));

                await dmChannel.SendMessageAsync("", embed: await GenerateEmbed( prop, newStatus ), components: cb.Build());
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
                var guild = _socketClient.GetGuild( sub.GuildId );
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
                await channel.SendMessageAsync("", embed: await GenerateEmbed( prop, newStatus ));
            }
            catch( Exception e )
            {
                _logger.LogError($"Failed to send channel message: {e}");
            }
        }
    }

    private async Task<Embed> GenerateEmbed( Proposal prop, string newStatus )
    {
        var title = ( prop.Status, newStatus ) switch
        {
            (_,"PROPOSAL_STATUS_PASSED") => "✅ PASSED",
            (_,"PROPOSAL_STATUS_REJECTED") => "❌ REJECTED",
            (_,"PROPOSAL_STATUS_VOTING_PERIOD") => "🕑 VOTING on",
            (null,_) => "New",
            _ => ""
        };

        var color = ( prop.Status, newStatus ) switch
        {
            (_,"PROPOSAL_STATUS_PASSED") => Color.Green,
            (_,"PROPOSAL_STATUS_REJECTED") => Color.Red,
            (_,"PROPOSAL_STATUS_VOTING_PERIOD") => Color.Blue,
            _ => Color.Default
        };
        
        var fields = new List<EmbedFieldBuilder>
        {
            new EmbedFieldBuilder()
                .WithName( "Chain name" )
                .WithValue( prop.Chain.Name ),
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
            eb.WithThumbnailUrl( await _imageFetcher.FetchImage( prop.Chain.ImageUrl, prop.Chain.Name ) );
        }

        return eb.Build();
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
