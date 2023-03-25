﻿using System.Text;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using EnkaAPI;
using Geno.Utils;
using Geno.Utils.Services;
using Geno.Utils.Services.Database;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using SDC_Sharp;
using SDC_Sharp.DiscordNet;
using SDC_Sharp.Types;
using Serilog;
using Serilog.Extensions.Logging;
using ShikimoriSharp;
using ShikimoriSharp.Bases;
using Logger = Microsoft.Extensions.Logging.Logger<Microsoft.Extensions.Logging.ILogger>;
using JsonLocalizationManager = Geno.Utils.Types.JsonLocalizationManager;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

var env = Utils.GetEnv();
var localizations = env.TryGetValue("LOCALS", out var p)
	? Path.GetFullPath(p)
	: Path.GetFullPath("../../", AppDomain.CurrentDomain.BaseDirectory) + "Localizations";

await using var service = new ServiceCollection()
	.AddSingleton(new DiscordSocketConfig
	{
		AlwaysDownloadUsers = false,
		AlwaysDownloadDefaultStickers = false,
		AlwaysResolveStickers = false,
		GatewayIntents = GatewayIntents.Guilds
		                 | GatewayIntents.GuildMembers
		                 | GatewayIntents.GuildMessages
		                 | GatewayIntents.GuildVoiceStates,
		MessageCacheSize = 1,
		LogLevel = LogSeverity.Verbose,
		LogGatewayIntentWarnings = false
	})
	.AddSingleton<DiscordShardedClient>()
	.AddSingleton<ILogger>(new LoggerConfiguration()
		.Enrich.FromLogContext()
		.MinimumLevel.Verbose()
		.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u4}]\t{Message:lj}{NewLine}{Exception}")
		.CreateLogger()
	)
	.AddSingleton<Microsoft.Extensions.Logging.ILogger>(provider => new Logger(new SerilogLoggerFactory(provider.GetRequiredService<ILogger>())))

	.AddSingleton(new InteractionServiceConfig
	{
		DefaultRunMode = RunMode.Async,
		EnableAutocompleteHandlers = true,
		LogLevel = LogSeverity.Verbose,
		UseCompiledLambda = true,
		LocalizationManager = new JsonLocalizationManager(localizations)
	})
	.AddSingleton<InteractionService>()
	.AddSingleton(MongoClientSettings.FromConnectionString(env["Mongo"]))
	.AddSingleton<IMongoClient, MongoClient>()
	.AddSingleton<DatabaseCache>()
	.AddSingleton<DatabaseProvider>()
	.AddSingleton<CommandHandlingService>()
	
	.AddSingleton<ClientEvents>()
	.AddSingleton<GuildEvents>()
	
	.AddSingleton(new SdcConfig { Token = env["Sdc"] })
	.AddSingleton<SdcSharpClient>()
	.AddSingleton<SdcServices>()

	.AddSingleton<EnkaApiClient>()
	
	.AddSingleton(new ClientSettings(
			"Geno",
			"mkGRM2ud5xmOqUl5bvZkUbFV-zqjQimkQ-W5hhPBFR0",
			"OlOUNsD14GN2TM6WHwaUaEuqrkFS7LGKJfwtHvyf6Ck"
		)
	)
	.AddSingleton<ShikimoriClient>()

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