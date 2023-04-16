using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

	//private static readonly Embed s_emptyEmbed = new EmbedBuilder().Build();

	private readonly DiscordShardedClient m_client;
	private readonly LocalizationManager m_localizationManager;
	private readonly IServiceProvider m_services;

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
		var (priv, safe) = FilterModules(modules);

		if (priv.TryGetValue(Category.Admin, out var module))
			await Interactions.AddModulesToGuildAsync(648571219674923008, true, module);

		Private = priv;
		await Interactions.AddModulesGloballyAsync(true, safe);

		ErrorResolver.Init(assembly, m_localizationManager);
		Responser.Init(m_localizationManager);
		GC.Collect();
	}

	private (IReadOnlyDictionary<Category, ModuleInfo[]>, ModuleInfo[]) FilterModules(ModuleInfo?[] modules)
	{
		var dict = new RefList<KeyValuePair<Category, LinkedList<ModuleInfo>>>(2);
		var safeArray = new RefList<ModuleInfo>(5);

		ref var start = ref MemoryMarshal.GetArrayDataReference(modules);
		ref var end = ref Unsafe.Add(ref start, modules.Length);

		while (Unsafe.IsAddressLessThan(ref start, ref end))
		{
			if (start is not null)
			{
				var attributes = new RefList<Attribute>(start.Attributes);
				if (attributes.FirstOrDefault(x => x is PrivateAttribute)
					    is PrivateAttribute privateAttribute && !privateAttribute.IsDefaultAttribute())
				{
					if (!dict.TryGetValue(privateAttribute.Categories, out var category))
						category = dict
							.Add(new KeyValuePair<Category, LinkedList<ModuleInfo>>(privateAttribute.Categories,
								new LinkedList<ModuleInfo>())).Value;

					category.AddLast(start);
				}
				else
				{
					safeArray.Add(start);
				}
			}

			start = ref Unsafe.Add(ref start, 1)!;
		}

		return (
			new Dictionary<Category, ModuleInfo[]>(dict.ToArray<Category, ModuleInfo>())
			{
				{
					Category.None, Array.Empty<ModuleInfo>()
				}
			}.AsReadOnly(),
			safeArray.ToArray()
		);
	}

	private void RegisterEvents()
	{
		m_client.InteractionCreated += OnInteractionCreated;
		Interactions.InteractionExecuted += InteractionExecuted;
		Interactions.Log += ClientEvents.OnLog;
	}

	private static async Task InteractionExecuted(ICommandInfo commandInfo,
		IInteractionContext context,
		IResult resultRaw)
	{
		if (resultRaw is Result result)
		{
			await context.Interaction.Respond(result.Builder, ephemeral: result.IsEphemeral, isDefered: result.IsDefered)
				.ConfigureAwait(false);
			return;
		}

		if (resultRaw.Error is null or InteractionCommandError.UnknownCommand)
			return;

		var embed = ErrorResolver.Resolve(resultRaw, commandInfo, context);

		await context.Interaction.Respond(embed, ephemeral: true).ConfigureAwait(false);
	}

	private async Task OnInteractionCreated(SocketInteraction arg)
	{
		try
		{
			var ctx = new ShardedInteractionContext(m_client, arg);
			await Interactions.ExecuteCommandAsync(ctx, m_services).ConfigureAwait(false);
		}
		catch (Exception e)
		{
			await ClientEvents.OnLog(new LogMessage(LogSeverity.Error, nameof(OnInteractionCreated), e.Message));
		}
	}
}