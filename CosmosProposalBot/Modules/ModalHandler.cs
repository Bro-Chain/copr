using System.Data.Common;
using System.Text.RegularExpressions;
using CosmosProposalBot.Configuration;
using CosmosProposalBot.Data;
using CosmosProposalBot.Data.Model;
using CosmosProposalBot.Model;
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

public class ModalHandler
{
    private readonly ILogger<ModalHandler> _logger;
    private readonly IOptions<BotOptions> _options;
    private readonly IServiceProvider _serviceProvider;

    private readonly Regex _chainNameRegex = new (@"^[A-Za-z][\w\d\-]{5,31}$");
    
    public ModalHandler( ILogger<ModalHandler> logger,
        IOptions<BotOptions> options,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _options = options;
        _serviceProvider = serviceProvider;
    }

    public async Task HandleModalAsync( SocketInteractionContext ctx, SocketModal modal )
    {
        switch( modal.Data.CustomId )
        {
            case "custom-chain":
                await HandleCustomChainModalAsync( ctx, modal );
                break;
            default:
                await modal.RespondAsync( "I don't know that command..." );
                break;
        }
    }
    
    private async Task HandleCustomChainModalAsync( SocketInteractionContext ctx, SocketModal modal )
    {
        await modal.RespondAsync( "Verifying...", ephemeral: true );

        var chainNameComponent = modal.Data.Components.FirstOrDefault( c => c.CustomId == "chain-name" );
        var restEndpointComponent = modal.Data.Components.FirstOrDefault( c => c.CustomId == "rest-endpoint" );
        var governanceUrlComponent = modal.Data.Components.FirstOrDefault( c => c.CustomId == "gov-url" );
        var imageUrlComponent = modal.Data.Components.FirstOrDefault( c => c.CustomId == "image-url" );
        
        if(chainNameComponent == null || restEndpointComponent == null)
        {
            await modal.FollowupAsync( "Input was malformed. Please contact my developers." );
            return;
        }

        if( !_chainNameRegex.IsMatch( chainNameComponent.Value ) )
        {
            await modal.FollowupAsync("Chain name is invalid. It must start with an alphabetic character, is otherwise alphanumeric plus dashes ('-'), and be between 6 and 32 characters long.", ephemeral: true);
            return;
        }

        if( string.IsNullOrEmpty(restEndpointComponent.Value) ||
            !restEndpointComponent.Value.StartsWith("https://") || 
            !Uri.TryCreate(restEndpointComponent.Value, UriKind.Absolute, out _))
        {
            await modal.FollowupAsync("Rest endpoint name is invalid. It must start with https://, be a valid address, and be between 6 and 32 characters long. If no port is specified, 443 is assumed.", ephemeral: true);
            return;
        }
        
        await using var scope = _serviceProvider.CreateAsyncScope();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var result = await RestRequestHelper.RequestWithRetry<BlockInfoResult>( httpClientFactory, restEndpointComponent.Value, "cosmos/base/tendermint/v1beta1/blocks/latest", _options.Value );
        if( result.Outcome == OutcomeType.Failure )
        {
            await modal.FollowupAsync( $"Failed to connect to the rest endpoint: {result.FinalException.Message}", ephemeral: true );
            return;
        }

        if( result.Result?.Block?.Header?.Height == null )
        {
            await modal.FollowupAsync( "Failed to connect to the rest endpoint: The endpoint did not return a valid block height.", ephemeral: true );
            return;
        }

        await modal.FollowupAsync( "Successfully validated the endpoint. Setting up chain tracking...", ephemeral: true );

        var dbContext = scope.ServiceProvider.GetRequiredService<CopsDbContext>();
        var existingChain = await dbContext.Chains.FirstOrDefaultAsync( c => c.Name == chainNameComponent.Value && 
                                                                             c.CustomForGuildId == ctx.Guild.Id );

        if( existingChain != null )
        {
            await modal.FollowupAsync( "There is already a chain registered by the same name. Either remove and recreate the custom chain tracking, or choose a different name.", ephemeral: true );
            return;
        }

        var newChain = new Chain
        {
            Name = chainNameComponent.Value,
            LinkPattern = governanceUrlComponent?.Value,
            ImageUrl = imageUrlComponent?.Value,
            CustomForGuildId = ctx.Guild.Id
        };
        newChain.Endpoints = new List<Endpoint>()
        {
            new()
            {
                Chain = newChain,
                Provider = Guid.NewGuid().ToString(),
                Type = EndpointType.Rest,
                Url = restEndpointComponent.Value
            }
        };
        dbContext.Chains.Add( newChain );
        dbContext.Endpoints.Add( newChain.Endpoints.First() );
        await dbContext.SaveChangesAsync();

        var eb = new EmbedBuilder()
            .WithTitle("Set up chain tracking")
            .WithThumbnailUrl( imageUrlComponent?.Value )
            .WithColor( Color.Green )
            .WithFields( 
                new EmbedFieldBuilder()
                    .WithName("Chain name")
                    .WithValue( chainNameComponent.Value ),
                new EmbedFieldBuilder()
                    .WithName("Chain id")
                    .WithValue( result.Result.Block.Header.ChainId ),
                new EmbedFieldBuilder()
                    .WithName("Last block time")
                    .WithValue( $"{result.Result.Block.Header.Time:yyyy-MM-dd HH:mm:ss} UTC" )
                    .WithIsInline(true),
                new EmbedFieldBuilder()
                    .WithName("Last block height")
                    .WithValue( result.Result.Block.Header.Height)
                    .WithIsInline( true ));
            
        var cb = new ComponentBuilder()
            .WithButton( "Subscribe Channel", "quick-subscribe-channel" )
            .WithButton( "Subscribe DM", "quick-subscribe-dm" );
        
        await modal.FollowupAsync( "Done!", embed: eb.Build(), ephemeral: false, components: cb.Build() );
    }
}
