using System.Collections;
using System.Text;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using EnkaAPI;
using Geno.Database;
using Geno.Events;
using Geno.Utils;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using SDC_Sharp;
using SDC_Sharp.DiscordNet;
using SDC_Sharp.Types;
using WargamingApi;
using WargamingApi.WorldOfTanksBlitz;
using RunMode = Discord.Interactions.RunMode;

Console.OutputEncoding = Encoding.UTF8;

var env = ((Hashtable) Environment.GetEnvironmentVariables()).Cast<DictionaryEntry>()
    .ToDictionary(
        kvp
            => (string) kvp.Key, kvp => (string) kvp.Value!);

var discordClient = new DiscordShardedClient(
    new DiscordSocketConfig
    {
        AlwaysDownloadUsers = false,
        AlwaysDownloadDefaultStickers = false,
        AlwaysResolveStickers = false,
        GatewayIntents = GatewayIntents.Guilds
                         | GatewayIntents.GuildMembers
                         | GatewayIntents.GuildMessages
                         | GatewayIntents.GuildVoiceStates,
        MessageCacheSize = 5,
        LogLevel = LogSeverity.Verbose,
        LogGatewayIntentWarnings = false
    }
);

var service = new ServiceCollection()
    .AddSingleton(discordClient)
    .AddSingleton(new InteractionService(discordClient, new InteractionServiceConfig
    {
        DefaultRunMode = RunMode.Async,
        EnableAutocompleteHandlers = true,
        LogLevel = LogSeverity.Verbose,
        //LocalizationManager = new JsonLocalizationManager(@"C:\projects\C#\Geno\Localizations", "interactions")
    }))
    .AddSingleton<IMongoClient>(new MongoClient(MongoClientSettings.FromConnectionString(env["Mongo"])))
    .AddSingleton<DatabaseCache>()
    .AddSingleton<DatabaseProvider>()
    .AddSingleton<CommandHandlingService>()
    .AddSingleton(new SdcConfig {Token = env["Sdc"]})
    .AddSingleton<SdcSharpClient>()
    .AddSingleton<SdcServices>()
    .AddSingleton<ClientEvents>()
    .AddSingleton<GuildEvents>()
    .AddSingleton(new WargamingApiClient("5ea271b8c279f6e11e334046af4cfce1"))
    .AddSingleton<WorldOfTanksBlitzClient>()
    .AddSingleton<EnkaApiClient>()
    .InitializeSdcServices()
    .BuildServiceProvider();

service.GetRequiredService<ClientEvents>();
service.GetRequiredService<GuildEvents>();

var bot = service.GetRequiredService<DiscordShardedClient>();

#if DEBUG
await bot.LoginAsync(TokenType.Bot, env["TEST"]);
#else
await bot.LoginAsync(TokenType.Bot, env["Geno"]);
#endif

await bot.StartAsync();

await Task.Delay(Timeout.Infinite);