using System.Reflection;
using CosmosProposalBot.Configuration;
using CosmosProposalBot.Modules;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CosmosProposalBot.Services;

public class DiscordBotService : IHostedService
{
    private readonly ILogger<DiscordBotService> _logger;
    private readonly IHostEnvironment _environment;
    private readonly IOptions<BotOptions> _options;
    private readonly IServiceProvider _services;
    private readonly DiscordSocketClient _discordClient;
    private readonly InteractionService _interactionService;

    public DiscordBotService( 
        ILogger<DiscordBotService> logger, 
        IHostEnvironment environment,
        IOptions<BotOptions> options,
        IServiceProvider services,
        DiscordSocketClient discordClient,
        InteractionService interactionService )
    {
        _logger = logger;
        _environment = environment;
        _options = options;
        _services = services;
        _discordClient = discordClient;
        _interactionService = interactionService;
    }
    
    public async Task StartAsync( CancellationToken cancellationToken )
    {
        try
        {
            _discordClient.Log += OnDiscordClientLog;
            _discordClient.Ready += OnDiscordClientReady;
            _interactionService.Log += OnDiscordClientLog;
            await _discordClient.LoginAsync( TokenType.Bot, _options.Value.DiscordApiToken );
            await _discordClient.StartAsync();
        }
        catch( Exception e )
        {
            _logger.LogError( e.Message );
            throw;
        }
    }

    private async Task OnDiscordClientLog( LogMessage logMessage )
    {
        switch( logMessage.Severity )
        {
            case LogSeverity.Critical:
                _logger.LogCritical( logMessage.Message );
                break;
            case LogSeverity.Error:
                _logger.LogError( logMessage.Message );
                break;
            case LogSeverity.Warning:
                _logger.LogWarning( logMessage.Message );
                break;
            case LogSeverity.Info:
                _logger.LogInformation( logMessage.Message );
                break;
            case LogSeverity.Debug:
                _logger.LogDebug( logMessage.Message );
                break;
            case LogSeverity.Verbose:
                _logger.LogTrace( logMessage.Message );
                break;
        }
    }

    private async Task OnDiscordClientReady()
    {
        _logger.LogInformation("Discord client ready...");

        _discordClient.SlashCommandExecuted += async command =>
        {
            var ctx = new SocketInteractionContext( _discordClient, command );
            await _interactionService.ExecuteCommandAsync( ctx, _services );
        };
        _discordClient.ModalSubmitted += OnModalSubmitted;
        _discordClient.ButtonExecuted += OnButtonExecuted;
        
        try
        {
            foreach (var discordClientGuild in _discordClient.Guilds)
            {
                _logger.LogInformation( $"{discordClientGuild.Name}: {discordClientGuild.Id}" );
            }
            await _interactionService.AddModulesAsync( Assembly.GetExecutingAssembly(), _services );
            if( _options.Value.DevelopmentGuildId.HasValue && _environment.IsDevelopment() )
            {
                await _interactionService.RegisterCommandsToGuildAsync( _options.Value.DevelopmentGuildId.Value );
            }
            else
            {
                await _interactionService.RegisterCommandsGloballyAsync();
            }
        }
        catch( Exception e )
        {
            _logger.LogError( e.Message );
            throw;
        }
    }

    private async Task OnModalSubmitted( SocketModal modal )
    {
        var ctx = new SocketInteractionContext( _discordClient, modal );
        await using var scope = _services.CreateAsyncScope();
        var modalHandler = scope.ServiceProvider.GetRequiredService<ModalHandler>();
        
        await modalHandler.HandleModalAsync( ctx, modal );
    }

    private async Task OnButtonExecuted( SocketMessageComponent component )
    {
        var ctx = new SocketInteractionContext( _discordClient, component );
        await using var scope = _services.CreateAsyncScope();
        var buttonHandler = scope.ServiceProvider.GetRequiredService<ButtonHandler>();
        
        await buttonHandler.HandleModalAsync( ctx, component );
    }

    public async Task StopAsync( CancellationToken cancellationToken )
    {
        await _discordClient.StopAsync();
    }
}
