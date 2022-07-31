using System;
using System.Threading;
using System.Threading.Tasks;
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

namespace Geno;

public static class Program
{
    public const string DefPrefix = "g-";

    internal static readonly DiscordShardedClient DiscordClient = new(
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

    private static readonly CommandService m_commands = new(
        new CommandServiceConfig
        {
            DefaultRunMode = RunMode.Async,
            IgnoreExtraArgs = false,
            LogLevel = LogSeverity.Verbose,
            ThrowOnError = true,
            CaseSensitiveCommands = false
        }
    );

    private static readonly InteractionService m_interactions = new(DiscordClient, new InteractionServiceConfig
    {
        DefaultRunMode = Discord.Interactions.RunMode.Async,
        EnableAutocompleteHandlers = true,
        LogLevel = LogSeverity.Verbose
    });

    internal static readonly IServiceProvider Service = new ServiceCollection()
        .AddSingleton(DiscordClient)
        .AddSingleton(m_interactions)
        .AddSingleton(m_commands)
        .AddSingleton<CommandHandlingService>()
        .AddSingleton<IMongoClient>(new MongoClient(MongoClientSettings.FromConnectionString("mongodb+srv://Geno:Atlas23Game@genodb.wrqdw.mongodb.net/?retryWrites=true&w=majority")))
        .AddSingleton(new SdcConfig
        {
            Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpZCI6Ijc1MDQxNTM1MDM0ODM4MjI0OSIsInBlcm1zIjowLCJpYXQiOjE2MDg1ODE0OTl9.M1q0XJ3iQw0lD5g-dgJabOmxU5auhNe_MM0IyuRAcK0"
        })
        .AddSingleton<SdcSharpClient>()
        .AddSingleton<SdcServices>()
        
        .InitializeSdcServices()
        .BuildServiceProvider();

    public static async Task Main(string[] args)
    {
        DiscordClient.ShardReady += ClientEvents.OnReady;
        DiscordClient.MessageReceived += GuildEvents.MessageReceived;
        DiscordClient.Log += Logger;

        await DiscordClient.LoginAsync(
            TokenType.Bot,
            args[0]
        );
        await DiscordClient.StartAsync();

        await Task.Delay(Timeout.Infinite);
    }

    private static Task Logger(LogMessage message)
    {
        Console.WriteLine(message.ToString());
        return Task.CompletedTask;
    }
}