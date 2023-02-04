using Discord;

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
		Embed embed,
		bool ephemeral = false,
		bool isDefered = false)
	{
		if (isDefered)
			return context.Interaction.ModifyOriginalResponseAsync(x =>
				{
					var flags = ((MessageFlags)x.Flags!)!;
					x.Embed = embed;
					x.AllowedMentions = AllowedMentions.None;
					x.Flags = ephemeral ? flags ^ MessageFlags.Ephemeral : flags | MessageFlags.Ephemeral;
				}
			);

		return context.Interaction.RespondAsync(
			embed: embed,
			allowedMentions: AllowedMentions.None,
			ephemeral: ephemeral
		);
	}
}