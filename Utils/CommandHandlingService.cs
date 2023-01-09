using System.Reflection;
using Discord;
using Discord.Extensions.Interactions;
using Discord.Interactions;
using Discord.WebSocket;
using Geno.Errors;
using Geno.Events;
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

		m_interactions.AddTypeConverter<ulong>(new UlongTypeConverter());

		await m_interactions.AddModulesAsync(assembly, m_services);
		await m_interactions.RegisterCommandsGloballyAsync();

		ErrorResolver.Init(assembly);
	}

	private void RegisterEvents()
	{
		m_client.InteractionCreated += OnInteractionCreated;
		m_interactions.InteractionExecuted += InteractionExecuted;
		m_interactions.Log += ClientEvents.OnLog;
	}

	private async Task InteractionExecuted(ICommandInfo commandInfo, IInteractionContext context, IResult result)
	{
		if (result.Error is not null)
		{
			var embed = ErrorResolver.Resolve(result, commandInfo, context);
			await context.Interaction.RespondAsync(embed: embed.Build(),
				allowedMentions: AllowedMentions.None,
				ephemeral: true);
		}
	}

	private async Task OnInteractionCreated(SocketInteraction arg)
	{
		//var ctx = arg.CreateGenericContext(m_client);
		var ctx = new ShardedInteractionContext(m_client, arg);
		await m_interactions.ExecuteCommandAsync(ctx, m_services);
	}
}