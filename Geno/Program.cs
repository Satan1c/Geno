using System.Text;
using Database;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using EnkaAPI;
using Geno.Handlers;
using Geno.Utils;
using Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using SDC_Sharp;
using SDC_Sharp.DiscordNet;
using SDC_Sharp.Types;
using Serilog;
using ShikimoriSharp;
using WaifuPicsApi;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Logger = Microsoft.Extensions.Logging.Logger<Microsoft.Extensions.Logging.ILogger>;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

var env = Utils.GetEnv();
var locals = Path.GetFullPath("../../../", AppDomain.CurrentDomain.BaseDirectory) + "Localizations";
var jsons = locals + "/json";
var csv = locals + "/csv";

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
	.AddSingleton(services => new InteractionService(
		services.GetRequiredService<DiscordShardedClient>(),
		new InteractionServiceConfig
		{
			DefaultRunMode = RunMode.Async,
			EnableAutocompleteHandlers = true,
			LogLevel = LogSeverity.Verbose,
			UseCompiledLambda = true,
			LocalizationManager = new CommandsLocalizationManager(jsons)
		})
	)
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
	.AddSingleton(new SdcSharpClient(new SdcConfig { Token = env["Sdc"] }))
	.AddSingleton<SdcServices>()
	.AddSingleton<EnkaApiClient>()
	.AddSingleton<ShikimoriService.ShikimoriClient>()
	.AddSingleton<WaifuClient>()
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