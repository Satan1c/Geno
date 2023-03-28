﻿using System.Reflection;
using Discord;
using Discord.Extensions.Interactions;
using Discord.Interactions;
using Discord.WebSocket;
using Geno.Responsers.Error;
using Geno.Responsers.Success;
using Geno.Utils.Extensions;
using Geno.Utils.Types;
using Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Geno.Handlers;

public class CommandHandlingService
{
	public static InteractionService Interactions = null!;
	public static IReadOnlyDictionary<Category, ModuleInfo[]> Private = null!;

	private static readonly Embed s_emptyEmbed = new EmbedBuilder().Build();

	private readonly DiscordShardedClient m_client;
	private readonly IServiceProvider m_services;
	private readonly LocalizationManager m_localizationManager;

	public CommandHandlingService(IServiceProvider services)
	{
		m_services = services;
		m_client = services.GetRequiredService<DiscordShardedClient>();
		m_localizationManager = services.GetRequiredService<LocalizationManager>();
		Interactions = services.GetRequiredService<InteractionService>();
	}

	public async Task InitializeAsync()
	{
		RegisterEvents();

		var assembly = Assembly.GetEntryAssembly()!;

		Interactions.AddTypeConverter<ulong>(new UlongTypeConverter());

		var modules = (await Interactions.AddModulesAsync(assembly, m_services)).ToArray();
		var safe = new LinkedList<ModuleInfo>();
		var priv = new Dictionary<Category, LinkedList<ModuleInfo>>();

		foreach (var m in modules)
		{
			if (m is null)
				continue;

			if (m.Attributes.FirstOrDefault(x => x is PrivateAttribute)
				    is PrivateAttribute privateAttribute && !privateAttribute.IsDefaultAttribute())
			{
				if (privateAttribute.Categories.HasCategory(Category.Admin))
					await Interactions.AddModulesToGuildAsync(648571219674923008, true, m);

				if (!priv.ContainsKey(privateAttribute.Categories))
					priv[privateAttribute.Categories] = new LinkedList<ModuleInfo>();

				priv[privateAttribute.Categories].AddLast(m);

				continue;
			}

			safe.AddLast(m);
		}

		Private = new Dictionary<Category, ModuleInfo[]>(
				priv
					.Select(k =>
						new KeyValuePair<Category, ModuleInfo[]>(k.Key, k.Value.ToArray())
					)
				) { { Category.None, Array.Empty<ModuleInfo>() } }
			.AsReadOnly();

		await Interactions.AddModulesGloballyAsync(true, safe.ToArray());

		ErrorResolver.Init(assembly, m_localizationManager);

		GC.Collect();
	}

	private void RegisterEvents()
	{
		m_client.InteractionCreated += OnInteractionCreated;
		Interactions.InteractionExecuted += InteractionExecuted;
		Interactions.Log += ClientEvents.OnLog;
	}

	private static Task InteractionExecuted(ICommandInfo commandInfo, IInteractionContext context, IResult resultRaw)
	{
		if (resultRaw is Result result)
			return context.Respond(result.Builder ?? s_emptyEmbed.ToEmbedBuilder(), result.IsEphemeral,
				result.IsDefered);

		if (resultRaw.Error is null)
			return Task.CompletedTask;

		var embed = ErrorResolver.Resolve(resultRaw, commandInfo, context);

		return context.Respond(embed, true);
	}

	private Task OnInteractionCreated(SocketInteraction arg)
	{
		var ctx = new ShardedInteractionContext(m_client, arg);
		return Interactions.ExecuteCommandAsync(ctx, m_services);
	}
}