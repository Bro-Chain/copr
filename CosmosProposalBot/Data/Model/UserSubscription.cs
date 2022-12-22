namespace CosmosProposalBot.Data.Model;

public class UserSubscription : Subscription
{
    public ulong DiscordUserId { get; set; }
    public string Username { get; set; }
    public string Discriminator { get; set; }
}
