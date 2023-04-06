using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Discord;
using Geno.Responsers.Success.Modules;
using Localization;

namespace Geno.Responsers.Success;

public static class Responser
{
	public static void Init(LocalizationManager localizationManager)
	{
		GenshinResponse.Init(localizationManager);
		GenshinResponse.Init(localizationManager);
		GenshinResponse.Init(localizationManager);
	}
	
	public static ValueTask Respond(this IInteractionContext context,
		EmbedBuilder embed,
		bool ephemeral = false,
		bool isDefered = false)
	{
		return context.Respond(embed.WithColor(embed.Color ?? new Color(43, 45, 49)).Build(), ephemeral, isDefered);
	}

	public static ValueTask Respond(this IInteractionContext context,
		EmbedBuilder[] embeds,
		bool ephemeral = false,
		bool isDefered = false)
	{
		var res = new Embed[embeds.Length];
		ref var start = ref MemoryMarshal.GetArrayDataReference(embeds);
		ref var resStart = ref MemoryMarshal.GetArrayDataReference(res);
		ref var end = ref Unsafe.Add(ref start, embeds.Length);

		while (Unsafe.IsAddressLessThan(ref start, ref end))
		{
			resStart = start.WithColor(start.Color ?? new Color(43, 45, 49)).Build();
			
			start = ref Unsafe.Add(ref start, 1);
			resStart = ref Unsafe.Add(ref resStart, 1);
		}
		
		return context.Respond(null, ephemeral, isDefered, res);
	}

	private static async ValueTask Respond(this IInteractionContext context,
		Embed? embed = null,
		bool ephemeral = false,
		bool isDefered = false,
		Embed[]? embeds = null)
	{
		if (isDefered)
		{
			await context.Interaction.ModifyOriginalResponseAsync(x =>
				{
					var flags = x.Flags.GetValueOrDefault() ?? MessageFlags.None;
					x.Embed = embed;
					x.Embeds = embeds;
					x.AllowedMentions = AllowedMentions.None;
					x.Flags = ephemeral ? flags ^ MessageFlags.Ephemeral : flags | MessageFlags.Ephemeral;
				}
			).ConfigureAwait(false);
			
			return;
		}

		try
		{
			await context.Interaction.RespondAsync(
				embed: embed,
				embeds: embeds,
				allowedMentions: AllowedMentions.None,
				ephemeral: ephemeral
			).ConfigureAwait(false);
		}
		catch (InvalidOperationException _)
		{
			await context.Respond(embed, ephemeral, true, embeds).ConfigureAwait(false);
		}
	}
}