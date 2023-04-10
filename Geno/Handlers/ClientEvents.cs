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
		var text = "";

		if (message.Exception is { StackTrace: { } })
		{
			text = message.Exception.StackTrace.Replace("\n", "\n\t\t\t");
			if (message.Exception is { InnerException.StackTrace: { } })
			{
				var innerRaw = message.Exception.InnerException.StackTrace.Replace("\n", "\n\t\t\t");
				text = string.Create(
					text.Length + innerRaw.Length + 2,
					(text, innerRaw), (span, source) =>
					{
						span[0] = ' ';
						span[1] = '\n';
						source.text.CopyTo(span[2..source.text.Length]);
						source.innerRaw.CopyTo(span[(source.text.Length + 3)..]);
					});
			}
		}

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