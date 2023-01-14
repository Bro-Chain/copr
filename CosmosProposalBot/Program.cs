using CosmosProposalBot;
using CosmosProposalBot.Configuration;
using CosmosProposalBot.Data;
using CosmosProposalBot.Modules;
using CosmosProposalBot.Services;
using CosmosProposalBot.Util;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddOptions<BotOptions>()
            .Bind(hostContext.Configuration.GetSection(nameof(BotOptions)))
            .ValidateDataAnnotations();
        
        var discordSocketConfig = new DiscordSocketConfig()
        {
            LogLevel = LogSeverity.Verbose,
            UseInteractionSnowflakeDate = false
        };
        services.AddSingleton(discordSocketConfig);
        services.AddSingleton<DiscordSocketClient>();
        var interactionServiceConfig = new InteractionServiceConfig()
        {
            LogLevel = LogSeverity.Verbose,
        };
        services.AddSingleton( interactionServiceConfig );
        services.AddSingleton<InteractionService>();
        services.AddTransient<ModalHandler>();
        services.AddTransient<ButtonHandler>();
        services.AddTransient<IImageFetcher,ImageFetcher>();
        services.AddTransient<IEventBroadcaster, EventBroadcaster>();
        services.AddTransient<IApiRequestHelper,ApiRequestHelper>();
        services.AddTransient<ISubscriptionHelper,SubscriptionHelper>();
        services.AddTransient<IPermissionHelper,PermissionHelper>();
        services.AddSingleton<IProposalCheckRunner, ProposalCheckRunner>();
        services.AddSingleton<IUpgradeTrackingRunner, UpgradeTrackingRunner>();
        
        services.AddDbContext<CopsDbContext>(options =>
            options
                .UseSqlServer( hostContext.Configuration.GetConnectionString("CopsDatabase"), builder =>
                {
                    builder.CommandTimeout( 60 );
                    builder.EnableRetryOnFailure( 5 );
                }));
        
        services.AddHttpClient();
        
        services.AddHostedService<UpdateChainListService>();
        services.AddHostedService<ProposalCheckService>();
        services.AddHostedService<UpgradeTrackingService>();
        services.AddHostedService<DiscordBotService>();
    })
    .Build();

await using( var scope = host.Services.CreateAsyncScope() )
{
    await CopsDbContext.Migrate( scope.ServiceProvider );
}

await host.RunAsync();
