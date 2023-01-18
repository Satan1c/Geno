using Discord;
using Discord.Interactions;

namespace Geno.Responses;

public static class Responser
{
	public static async Task Respond(this ShardedInteractionContext context,
		EmbedBuilder embed,
		bool ephemeral = false,
		bool isDefered = false)
	{
		var em = embed.Build();

		if (isDefered)
		{
			await context.Interaction.ModifyOriginalResponseAsync(x =>
				{
					x.Embed = em;
					x.AllowedMentions = AllowedMentions.None;
				}
			);
			return;
		}

		await context.Interaction.RespondAsync(embed: em,
			allowedMentions: AllowedMentions.None,
			ephemeral: ephemeral
		);
	}
}