﻿using Discord;

namespace Geno.Responses;

public static class Responser
{
	public static Task Respond(this IInteractionContext context,
		EmbedBuilder embed,
		bool ephemeral = false,
		bool isDefered = false)
	{
		return context.Respond(embed.Build(), ephemeral, isDefered);
	}
	
	public static Task Respond(this IInteractionContext context,
		EmbedBuilder[] embeds,
		bool ephemeral = false,
		bool isDefered = false)
	{
		return context.Respond(null, ephemeral, isDefered,embeds.Select(x => x.Build()).ToArray());
	}

	private static Task Respond(this IInteractionContext context,
		Embed? embed = null,
		bool ephemeral = false,
		bool isDefered = false,
		Embed[] embeds = null)
	{
		if (isDefered)
			return context.Interaction.ModifyOriginalResponseAsync(x =>
				{
					var flags = x.Flags.GetValueOrDefault() ?? MessageFlags.None;
					x.Embed = embed;
					x.Embeds = embeds;
					x.AllowedMentions = AllowedMentions.None;
					x.Flags = ephemeral ? flags ^ MessageFlags.Ephemeral : flags | MessageFlags.Ephemeral;
				}
			);

		return context.Interaction.RespondAsync(
			embed: embed,
			embeds: embeds,
			allowedMentions: AllowedMentions.None,
			ephemeral: ephemeral
		);
	}
}