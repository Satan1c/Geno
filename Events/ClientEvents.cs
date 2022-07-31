using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Geno.Utils;
using Microsoft.Extensions.DependencyInjection;
// using WorldOfTanksBlitz;
// using WorldOfTanksBlitz.Types.Enums;

namespace Geno.Events;

public static class ClientEvents
{
    public static async Task OnReady(DiscordSocketClient client)
    {
        // Program.Service.GetRequiredService<WorldOfTanksBlitzClient>().InitServices(Service.Accounts);
        await Program.Service.GetRequiredService<CommandHandlingService>().InitializeAsync();
        Console.WriteLine("Ready");
    }
}