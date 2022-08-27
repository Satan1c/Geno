using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using IResult = Discord.Commands.IResult;

namespace Geno.Utils;

public class CommandHandlingService
{
    private readonly DiscordShardedClient m_client;
    private readonly CommandService m_commands;
    private readonly InteractionService m_interactions;
    private readonly IServiceProvider m_services;

    public CommandHandlingService(IServiceProvider services)
    {
        m_services = services;
        m_client = services.GetRequiredService<DiscordShardedClient>();
        m_commands = services.GetRequiredService<CommandService>();
        m_interactions = services.GetRequiredService<InteractionService>();
        // services.GetRequiredService<InteractionService>();
    }

    public async Task InitializeAsync()
    {
        RegisterEvents();

        var assembly = Assembly.GetEntryAssembly();

        await m_commands.AddModulesAsync(assembly, m_services);
        await m_interactions.AddModulesAsync(assembly, m_services);
        await m_interactions.RegisterCommandsGloballyAsync(true);
    }

    private Task MessageReceivedAsync(SocketMessage rawMessage)
    {
        _ = Task.Run(async () =>
        {
            if (rawMessage.Source != MessageSource.User)
                return;

            var userMessage = rawMessage as SocketUserMessage;
            var prefixEndPosition = 0;

            if (HasPrefixOrMention(userMessage!, ref prefixEndPosition))
                return;

            await m_commands.ExecuteAsync(
                new ShardedCommandContext(m_client, userMessage),
                prefixEndPosition,
                m_services,
                MultiMatchHandling.Best
            );
        });

        return Task.CompletedTask;
    }

    private static async Task CommandExecutedAsync(
        Optional<CommandInfo> command,
        ICommandContext context,
        IResult result
    )
    {
        if (!command.IsSpecified)
        {
            await context.Message.ReplyAsync("Unspecified", allowedMentions: AllowedMentions.None);
            return;
        }

        if (!result.IsSuccess)
            Console.WriteLine("Error:\n" + result);
    }

    private bool HasPrefixOrMention(SocketUserMessage userMessage, ref int prefixEndPosition)
    {
        return !userMessage.HasMentionPrefix(m_client.CurrentUser, ref prefixEndPosition)
               && !userMessage.HasStringPrefix("g-", ref prefixEndPosition);
    }

    private void RegisterEvents()
    {
        /*m_commands.CommandExecuted += CommandExecutedAsync;*/
        /*m_client.MessageReceived += MessageReceivedAsync;*/
        m_client.InteractionCreated += OnInteractionCreated;
        m_interactions.InteractionExecuted += InteractionExecuted;
    }

    private async Task InteractionExecuted(ICommandInfo commandInfo, IInteractionContext context, Discord.Interactions.IResult result)
    {
        if (result.Error is not null)
        {
            var message = result.Error switch
            {
                InteractionCommandError.UnknownCommand => "Unknown command",
                InteractionCommandError.ConvertFailed => "None ConvertFailed",
                InteractionCommandError.BadArgs => "Invalid number or arguments",
                InteractionCommandError.Exception => $"Command exception: {result.ErrorReason}",
                InteractionCommandError.Unsuccessful => "Command could not be executed",
                InteractionCommandError.UnmetPrecondition => $"Unmet Precondition: {result.ErrorReason}",
                InteractionCommandError.ParseFailed => "None ParseFailed",
                null => "mull",
                _ => throw new ArgumentOutOfRangeException()
            };

            await context.Interaction.RespondAsync(message,
                allowedMentions: AllowedMentions.None,
                ephemeral: true);
        }
    }

    private async Task OnInteractionCreated(SocketInteraction arg)
    {
        await m_interactions.ExecuteCommandAsync(new ShardedInteractionContext(m_client, arg), m_services);
    }
}