using System.Text;
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
using JsonLocalizationManager = Geno.Utils.Types.JsonLocalizationManager;

Console.OutputEncoding = Encoding.UTF8;

var localizations = Path.GetFullPath("../../../../", AppDomain.CurrentDomain.BaseDirectory) + "/Localizations";

var env = Utils.GetEnv();

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
		MessageCacheSize = 5,
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
	.AddSingleton(new SdcConfig { Token = env["Sdc"] })
	.AddSingleton<SdcSharpClient>()
	.AddSingleton<SdcServices>()
	.AddSingleton<ClientEvents>()
	.AddSingleton<GuildEvents>()
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