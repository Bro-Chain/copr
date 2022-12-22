namespace CosmosProposalBot.Data.Model;

public class ChannelSubscription : Subscription 
{
    public ulong GuildId { get; set; }
    public ulong DiscordChannelId { get; set; }
}
