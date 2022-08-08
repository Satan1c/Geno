using System.Collections;
using System.Text;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Geno.Events;
using Geno.Utils;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using SDC_Sharp;
using SDC_Sharp.DiscordNet;
using SDC_Sharp.Types;
using RunMode = Discord.Commands.RunMode;

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
        LogLevel = LogSeverity.Verbose
    }
);

var service = new ServiceCollection()
    .AddSingleton(discordClient)
    .AddSingleton(new InteractionService(discordClient, new InteractionServiceConfig
    {
        DefaultRunMode = RunMode.Async,
        EnableAutocompleteHandlers = true,
        LogLevel = LogSeverity.Verbose
    }))
    .AddSingleton(new CommandService(
        new CommandServiceConfig
        {
            DefaultRunMode = Discord.Commands.RunMode.Async,
            IgnoreExtraArgs = false,
            LogLevel = LogSeverity.Verbose,
            ThrowOnError = true,
            CaseSensitiveCommands = false
        }))
    .AddSingleton<IMongoClient>(new MongoClient(MongoClientSettings.FromConnectionString(env["Mongo"])))
    
    .AddSingleton<CommandHandlingService>()
    .AddSingleton(new SdcConfig {Token = env["Sdc"]})
    .AddSingleton<SdcSharpClient>()
    .AddSingleton<SdcServices>()
    .AddSingleton<ClientEvents>()
    .AddSingleton<GuildEvents>()
    
    .InitializeSdcServices()
    .BuildServiceProvider();

service.GetRequiredService<ClientEvents>();
service.GetRequiredService<GuildEvents>();

var bot = service.GetRequiredService<DiscordShardedClient>();
await bot.LoginAsync(TokenType.Bot, env["Geno"]);
await bot.StartAsync();

await Task.Delay(Timeout.Infinite);