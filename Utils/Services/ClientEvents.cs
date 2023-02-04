using Discord;
using Discord.WebSocket;
using Serilog;

namespace Geno.Utils.Services;

public class ClientEvents
{
	private static ILogger s_logger = null!;
	private readonly DiscordShardedClient m_client;
	private readonly CommandHandlingService m_handlingService;

	public ClientEvents(DiscordShardedClient client, ILogger logger,
		CommandHandlingService handlingService)
	{
		m_client = client;
		s_logger = logger;
		m_handlingService = handlingService;
		m_client.ShardReady += OnReady;
		m_client.Log += OnLog;
	}

	public static Task OnLog(LogMessage log)
	{
		var source = log.Source;
		var message = log.Message;
		var exceptionType = log.Exception?.GetType();
		var stackTrace = log.Exception?.StackTrace?.Replace("\n", "\n\t\t\t");

		switch (log.Severity)
		{
			case LogSeverity.Critical:
				s_logger.Fatal(
					"{Source}\t{Exception}\t{Trace}",
					source,
					exceptionType,
					stackTrace);
				break;
			case LogSeverity.Error:
				s_logger.Error(
					"{Source}\t{Exception}\t{Trace}",
					source,
					exceptionType,
					stackTrace);
				break;
			case LogSeverity.Warning:
				s_logger.Warning(
					"{Source}\t{Message}",
					source,
					message);
				break;
			case LogSeverity.Info:
				s_logger.Information(
					"{Source}\t{Message}",
					source,
					message);
				break;
			case LogSeverity.Verbose:
				s_logger.Verbose(
					"{Source}\t{Message}",
					source,
					log.Message);
				break;
			case LogSeverity.Debug:
				s_logger.Debug(
					"{Source}\t{Message}",
					source,
					message);
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		return Task.CompletedTask;
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