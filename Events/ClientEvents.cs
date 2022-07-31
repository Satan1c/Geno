using Discord;
using Discord.WebSocket;
using Geno.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Geno.Events;

public class ClientEvents
{
    private readonly CommandHandlingService m_handlingService;
    public ClientEvents(IServiceProvider services) 
    {
        m_handlingService = services.GetRequiredService<CommandHandlingService>();

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
        await m_handlingService.InitializeAsync();
        Console.WriteLine("Ready");
    }
}