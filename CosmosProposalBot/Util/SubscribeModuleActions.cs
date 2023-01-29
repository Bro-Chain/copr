using System.Text;
using CosmosProposalBot.Configuration;
using CosmosProposalBot.Data;
using CosmosProposalBot.Data.Model;
using CosmosProposalBot.Util;
using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CosmosProposalBot.Modules;

public interface ISubscribeModuleActions
{
    Task SupportedChains(IInteractionContext context);
    Task SubscribePrivate( IInteractionContext context, string chainName );
    Task SubscribeChannel( IInteractionContext context, string chainName );
}

public class SubscribeModuleActions : ISubscribeModuleActions
{
    private readonly ILogger<SubscribeModuleActions> _logger;
    private readonly IOptions<BotOptions> _options;
    private readonly IServiceProvider _serviceProvider;

    public SubscribeModuleActions( ILogger<SubscribeModuleActions> logger,
        IOptions<BotOptions> options,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _options = options;
        _serviceProvider = serviceProvider;
    }

    public async Task SupportedChains(IInteractionContext context)
    {
        try
        {
            await context.Interaction.DeferAsync();
            
            await using var scope = _serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CopsDbContext>();
            var guildSpecificChains = dbContext.Chains
                .Where( c => c.CustomForGuildId == context.Guild.Id )
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

            await context.Interaction.FollowupAsync($"", embed: eb.Build());
        }
        catch( Exception e )
        {
            Console.WriteLine( e );
            throw;
        }
    }

    public async Task SubscribePrivate( IInteractionContext context, string chainName )
    {
        try
        {
            await context.Interaction.DeferAsync( ephemeral: true );

            await using var scope = _serviceProvider.CreateAsyncScope();
            var subscriptionHelper = scope.ServiceProvider.GetRequiredService<ISubscriptionHelper>();
            
            await subscriptionHelper.SubscribeDm( context, chainName );
        }
        catch( Exception e )
        {
            Console.WriteLine( e );
            throw;
        }
    }

    public async Task SubscribeChannel( IInteractionContext context, string chainName )
    {
        try
        {
            await context.Interaction.DeferAsync();

            await using var scope = _serviceProvider.CreateAsyncScope();
            var subscriptionHelper = scope.ServiceProvider.GetRequiredService<ISubscriptionHelper>();
            
            await subscriptionHelper.SubscribeChannel( context, chainName );
        }
        catch( Exception e )
        {
            Console.WriteLine( e );
            throw;
        }
    }
}
