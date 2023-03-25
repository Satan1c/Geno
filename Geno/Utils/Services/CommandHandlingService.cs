using System.Reflection;
using Discord;
using Discord.Extensions.Interactions;
using Discord.Interactions;
using Discord.WebSocket;
using Geno.Errors;
using Geno.Responses;
using Geno.Utils.Extensions;
using Geno.Utils.Types;

namespace Geno.Utils.Services;

public class CommandHandlingService
{
	internal static InteractionService Interactions = null!;
	internal static IReadOnlyDictionary<Category, ModuleInfo[]> Private = null!;
	
	private readonly DiscordShardedClient m_client;
	private readonly IServiceProvider m_services;

	private static readonly Embed s_emptyEmbed = new EmbedBuilder().Build();

	public CommandHandlingService(IServiceProvider services, DiscordShardedClient client,
		InteractionService interactions)
	{
		m_services = services;
		m_client = client;
		Interactions = interactions;
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

			var attr = m.Attributes.FirstOrDefault(x => x is PrivateAttribute);

			if (attr != null && !attr.IsDefaultAttribute())
			{
				var attribute = (PrivateAttribute)attr;

				if (attribute.Categories.HasCategory(Category.Admin))
					await Interactions.AddModulesToGuildAsync(648571219674923008, true, m);

				if (!priv.ContainsKey(attribute.Categories))
					priv[attribute.Categories] = new LinkedList<ModuleInfo>();

				priv[attribute.Categories].AddLast(m);

				continue;
			}

			safe.AddLast(m);
		}

		Private = new Dictionary<Category, ModuleInfo[]>(priv.Select(k
				=> new KeyValuePair<Category, ModuleInfo[]>(k.Key, k.Value.ToArray())))
			{
				{ Category.None, Array.Empty<ModuleInfo>() }
			}
			.AsReadOnly();

		await Interactions.AddModulesGloballyAsync(true, safe.ToArray());

		ErrorResolver.Init(assembly);

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
			return context.Respond(result.Builder ?? s_emptyEmbed.ToEmbedBuilder(), result.IsEphemeral, result.IsDefered);

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