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
    private readonly Dictionary<string, string> _proposalTypeMapping = new Dictionary<string, string>
    {
        {"/cosmos.distribution.v1beta1.CommunityPoolSpendProposal", "Software Upgrade"},
        {"/cosmos.gov.v1beta1.TextProposal", "Text"},
        {"/cosmos.params.v1beta1.ParameterChangeProposal", "Parameter Change"},
        {"/cosmos.upgrade.v1beta1.SoftwareUpgradeProposal", "Software Upgrade"},
        {"/cosmos.upgrade.v1beta1.CancelSoftwareUpgradeProposal", "Cancel Software Upgrade"},
        {"/cosmos.upgrade.v1beta1.PlanChangeProposal", "Plan Change"},
        {"/cosmos.upgrade.v1beta1.CancelPlanChangeProposal", "Cancel Plan Change"},

        {"/cosmwasm.wasm.v1.StoreCodeProposal", "WASM Store Code"},

        {"/ibc.core.client.v1.ClientUpdateProposal", "IBC Client Update"},

        {"/acrechain.erc20.v1.RegisterCoinProposal", "Register Coin"},

        {"/agoric.swingset.CoreEvalProposal", "Core Eval"},

        {"/canto.govshuttle.v1.LendingMarketProposal", "Lending Market"},

        {"/comdex.asset.v1beta1.AddAssetsProposal", "Add Asset"},
        {"/comdex.bandoracle.v1beta1.FetchPriceProposal", "Fetch Price"},
        
        {"/Switcheo.carbon.liquiditypool.SetRewardCurveProposal", "Set Reward Curve"},
        {"/Switcheo.carbon.liquiditypool.LinkPoolProposal", "Link Pool"},
        {"/Switcheo.carbon.liquiditypool.UnlinkPoolProposal", "Unlink Pool"},
        {"/Switcheo.carbon.liquiditypool.SetPoolFeeProposal", "Set Pool Fee"},
        {"/Switcheo.carbon.liquiditypool.SetPoolFeeAddressProposal", "Set Pool Fee Address"},
        {"/Switcheo.carbon.liquiditypool.SetPoolFeeRateProposal", "Set Pool Fee Rate"},
        {"/Switcheo.carbon.liquiditypool.SetPoolFeeCapProposal", "Set Pool Fee Cap"},
        {"/Switcheo.carbon.liquiditypool.SetPoolFeeCollectorProposal", "Set Pool Fee Collector"},
        {"/Switcheo.carbon.liquiditypool.SetPoolFeeCollectorAddressProposal", "Set Pool Fee Collector Address"},
        {"/Switcheo.carbon.liquiditypool.SetPoolFeeCollectorRateProposal", "Set Pool Fee Collector Rate"},
        {"/Switcheo.carbon.liquiditypool.SetPoolFeeCollectorCapProposal", "Set Pool Fee Collector Cap"},
        {"/Switcheo.carbon.liquiditypool.SetPoolFeeCollectorEnabledProposal", "Set Pool Fee Collector Enabled"},
        {"/Switcheo.carbon.liquiditypool.SetPoolFeeCollectorDisabledProposal", "Set Pool Fee Collector Disabled"},

        // {"1", "Software Upgrade"},
        // {"1", "Software Upgrade"},
        // {"1", "Software Upgrade"},
        // {"1", "Software Upgrade"}
    };

        
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
