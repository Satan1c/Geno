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
	internal static InteractionService Interactions = null!;
	private readonly IServiceProvider m_services;

	internal static IReadOnlyDictionary<Category, ModuleInfo[]> Private = null!;

	public CommandHandlingService(IServiceProvider services)
	{
		m_services = services;
		m_client = services.GetRequiredService<DiscordShardedClient>();
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

			var attr = m.Attributes.FirstOrDefault(x => x is PrivateAttribute);

			if (attr != null && !attr.IsDefaultAttribute())
			{
				var attribute = ((PrivateAttribute)attr);
				
				if (attribute.Categories.HasCategory(Category.Admin))
				{
					await Interactions.AddModulesToGuildAsync(648571219674923008, true, m);
					
					continue;
				}

				if (!priv.ContainsKey(attribute.Categories))
					priv[attribute.Categories] = new LinkedList<ModuleInfo>();
				
				priv[attribute.Categories].AddLast(m);
				
				continue;
			}

			safe.AddLast(m);
		}

		Private = new Dictionary<Category, ModuleInfo[]>(
			priv.Select((k) => 
				new KeyValuePair<Category, ModuleInfo[]>(k.Key, k.Value.ToArray())))
			.AsReadOnly();
		
		await Interactions.AddModulesGloballyAsync(true, safe.ToArray());

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