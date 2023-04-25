using Discord;
using Discord.WebSocket;
using Serilog;
using Serilog.Events;

namespace Geno.Handlers;

public class ClientEvents
{
	private static ILogger? s_logger;
	private readonly DiscordShardedClient m_client;
	private readonly CommandHandlingService m_handlingService;

	public ClientEvents(DiscordShardedClient client,
		ILogger logger,
		CommandHandlingService handlingService)
	{
		m_client = client;
		s_logger = logger;
		m_handlingService = handlingService;
		m_client.ShardReady += OnReady;
		m_client.Log += OnLog;
	}

	public static Task OnLog(LogMessage message)
	{
		var trace = message.Exception?.StackTrace?.Replace("\n", "\n\t\t\t");
		trace = trace is null ? "" : trace + '\n';
		var inner = message.Exception?.InnerException?.StackTrace?.Replace("\n", "\n\t\t\t");

		var text = $"{trace}{inner ?? ""}";

		s_logger?.Write(
			SeverityToLevel(message.Severity),
			message.Exception,
			"[{Source}]\t{Message} {Trace}",
			message.Source,
			message.Message,
			text);

		return Task.CompletedTask;
	}

	private static LogEventLevel SeverityToLevel(LogSeverity severity)
	{
		return severity switch
		{
			LogSeverity.Critical => LogEventLevel.Fatal,
			LogSeverity.Error => LogEventLevel.Error,
			LogSeverity.Warning => LogEventLevel.Warning,
			LogSeverity.Info => LogEventLevel.Information,
			LogSeverity.Verbose => LogEventLevel.Verbose,
			LogSeverity.Debug => LogEventLevel.Debug,
			_ => LogEventLevel.Verbose
		};
	}

	private async Task OnReady(DiscordSocketClient client)
	{
		try
		{
			await m_handlingService.InitializeAsync();

			m_client.ShardReady -= OnReady;
		}
		catch (Exception e)
		{
			await OnLog(new LogMessage(LogSeverity.Error, nameof(OnReady), e.Message, e));
		}
	}
}