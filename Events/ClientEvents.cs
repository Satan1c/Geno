using Discord;
using Discord.WebSocket;
using Geno.Utils;
using Microsoft.Extensions.DependencyInjection;
using WargamingApi.WorldOfTanksBlitz;
using WargamingApi.WorldOfTanksBlitz.Types.Enums;

namespace Geno.Events;

public class ClientEvents
{
    private readonly WorldOfTanksBlitzClient m_blitzClient;
    private readonly CommandHandlingService m_handlingService;

    public ClientEvents(IServiceProvider services)
    {
        m_handlingService = services.GetRequiredService<CommandHandlingService>();
        m_blitzClient = services.GetRequiredService<WorldOfTanksBlitzClient>();

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
    }
}