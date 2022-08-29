using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.Extensions.Interactions;
using Discord.WebSocket;
using Geno.Errors;
using Microsoft.Extensions.DependencyInjection;

namespace Geno.Utils;

public class CommandHandlingService
{
    private readonly DiscordShardedClient m_client;
    private readonly InteractionService m_interactions;
    private readonly IServiceProvider m_services;

    public CommandHandlingService(IServiceProvider services)
    {
        m_services = services;
        m_client = services.GetRequiredService<DiscordShardedClient>();
        m_interactions = services.GetRequiredService<InteractionService>();
    }

    public async Task InitializeAsync()
    {
        RegisterEvents();

        var assembly = Assembly.GetEntryAssembly()!;
        await m_interactions.AddModulesAsync(assembly, m_services);
        await m_interactions.RegisterCommandsGloballyAsync();
        
        ErrorResolver.Init(assembly);
    }

    private void RegisterEvents()
    {
        m_client.InteractionCreated += OnInteractionCreated;
        m_interactions.InteractionExecuted += InteractionExecuted;
    }

    private async Task InteractionExecuted(ICommandInfo commandInfo, IInteractionContext context, Discord.Interactions.IResult result)
    {
        if (result.Error is not null)
        {
            var message = ErrorResolver.Resolve(result, commandInfo, context);
            await context.Interaction.RespondAsync(message,
                allowedMentions: AllowedMentions.None,
                ephemeral: true);
        }
    }

    private async Task OnInteractionCreated(SocketInteraction arg)
    {
        //await m_interactions.ExecuteCommandAsync(new ShardedInteractionContext(m_client, arg), m_services);
        await m_interactions.ExecuteCommandAsync(arg.CreateGenericContext(m_client), m_services);
    }
}