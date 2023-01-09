using Discord;
using Discord.WebSocket;
using Geno.Utils;
using Serilog;

namespace Geno.Events;

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

        var client = services.GetRequiredService<DiscordShardedClient>();
        client.ShardReady += OnReady;
        client.Log += OnLog;
    }

    public Task OnLog(LogMessage message)
    {
        Console.WriteLine(message.ToString());
        return Task.CompletedTask;
    }

    public async Task OnReady(DiscordSocketClient client)
    {
        m_blitzClient.InitServices(Service.All);
        await m_handlingService.InitializeAsync();
        Console.WriteLine("Ready");
        GC.Collect();
    }
}