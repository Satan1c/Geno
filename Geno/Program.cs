﻿using System.Text;
using Database;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using EnkaAPI;
using Geno.Handlers;
using Geno.Utils;
using HoYoLabApi;
using HoYoLabApi.GenshinImpact;
using HoYoLabApi.HonkaiStarRail;
using Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using SDC_Sharp;
using SDC_Sharp.DiscordNet;
using SDC_Sharp.Types;
using Serilog;
using ShikimoriService;
using ShikimoriSharp.Bases;
using WaifuPicsApi;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Logger = Microsoft.Extensions.Logging.Logger<Microsoft.Extensions.Logging.ILogger>;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

var env = Utils.GetEnv();
var locals = string.Concat(Path.GetFullPath("../../", AppDomain.CurrentDomain.BaseDirectory), "Localizations");
var jsons = string.Concat(locals, "/json");
var csv = string.Concat(locals, "/csv");

await using var service = new ServiceCollection()
	.AddSingleton(new DiscordShardedClient(new DiscordSocketConfig
	{
		AlwaysDownloadUsers = false,
		AlwaysDownloadDefaultStickers = false,
		AlwaysResolveStickers = false,
		GatewayIntents = GatewayIntents.Guilds
		                 | GatewayIntents.GuildInvites
		                 | GatewayIntents.GuildMembers
		                 | GatewayIntents.GuildMessages
		                 | GatewayIntents.GuildVoiceStates,
		MessageCacheSize = 1,
		LogLevel = LogSeverity.Info,
		LogGatewayIntentWarnings = false
	}))
	.AddSingleton(new InteractionServiceConfig
	{
		DefaultRunMode = RunMode.Async,
		EnableAutocompleteHandlers = true,
		LogLevel = LogSeverity.Verbose,
		UseCompiledLambda = true,
		LocalizationManager = new CommandsLocalizationManager(jsons)
	})
	.AddSingleton<InteractionService>()
	.AddSingleton<Serilog.ILogger>(new LoggerConfiguration()
		.Enrich.FromLogContext()
		.MinimumLevel.Verbose()
		.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u4}]\t{Message:lj}{NewLine}{Exception}")
		.CreateLogger()
	)
	.AddSingleton<ILogger>(_ => NullLogger.Instance)
	.AddSingleton(new LocalizationManager(csv))
	.AddSingleton<IMongoClient>(new MongoClient(MongoClientSettings.FromConnectionString(env["Mongo"])))
	.AddSingleton<DatabaseProvider>()
	.AddSingleton<CommandHandlingService>()
	.AddSingleton<ClientEvents>()
	.AddSingleton<GuildEvents>()
	.AddSingleton<SdcServices>()
	.AddSingleton<EnkaApiClient>()
	.AddSingleton<WaifuClient>()
	.AddSingleton(new ClientSettings(
		env["ShikimoriClientName"],
		env["ShikimoriClientId"],
		env["ShikimoriClientSecret"]
	))
	.AddSingleton<ShikimoriSharp.ShikimoriClient>()
	.AddSingleton<ShikimoriClient>()
	.AddSingleton<IHoYoLabClient, HoYoLabClient>()
	.AddSingleton<HonkaiStarRailService>()
	.AddSingleton<GenshinImpactService>()
	.AddSingleton<ISdcSharpClient>(new SdcSharpClient(new SdcConfig { Token = env["Sdc"] }))
	.AddSingleton<ISdcServices, SdcServices>()
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