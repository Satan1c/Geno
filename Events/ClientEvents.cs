using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Geno.Utils;
using Microsoft.Extensions.DependencyInjection;
// using WorldOfTanksBlitz;
// using WorldOfTanksBlitz.Types.Enums;

namespace Geno.Events;

public class ClientEvents
{
    private readonly CommandHandlingService m_handlingService;
    public ClientEvents(CommandHandlingService handlingService) 
        => m_handlingService = handlingService;

    public async Task OnReady(DiscordSocketClient client)
    {
        await m_handlingService.InitializeAsync();
        Console.WriteLine("Ready");
    }
}