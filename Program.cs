using System.Collections;
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

var env = ((Hashtable) Environment.GetEnvironmentVariables()).
    Cast<DictionaryEntry>()
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

var commands = new CommandService(
    new CommandServiceConfig
    {
        DefaultRunMode = RunMode.Async,
        IgnoreExtraArgs = false,
        LogLevel = LogSeverity.Verbose,
        ThrowOnError = true,
        CaseSensitiveCommands = false
    });

var interactions = new InteractionService(discordClient, new InteractionServiceConfig
{
    DefaultRunMode = Discord.Interactions.RunMode.Async,
    EnableAutocompleteHandlers = true,
    LogLevel = LogSeverity.Verbose
});

var service = new ServiceCollection()
    .AddSingleton(discordClient)
    .AddSingleton(interactions)
    .AddSingleton(commands)
    .AddSingleton<CommandHandlingService>()
    .AddSingleton<IMongoClient>(new MongoClient(MongoClientSettings.FromConnectionString(env["Mongo"])))
    .AddSingleton(new SdcConfig {Token = env["Sdc"]})
    .AddSingleton<SdcSharpClient>()
    .AddSingleton<SdcServices>()
    .AddSingleton<ClientEvents>()
    .AddSingleton<GuildEvents>()
    .InitializeSdcServices()
    .BuildServiceProvider();

discordClient.ShardReady += service.GetRequiredService<ClientEvents>().OnReady;
discordClient.MessageReceived += service.GetRequiredService<GuildEvents>().MessageReceived;
discordClient.Log += (message =>
{
    Console.WriteLine(message.ToString());
    return Task.CompletedTask;
});

await discordClient.LoginAsync(TokenType.Bot, env["Geno"]);
await discordClient.StartAsync();

await Task.Delay(Timeout.Infinite);