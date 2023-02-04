using Discord;

namespace Geno.Responses;

public static class Responser
{
	public static Task Respond(this ShardedInteractionContext context,
		EmbedBuilder embed,
		bool ephemeral = false,
		bool isDefered = false)
	{
		var em = embed.Build();

		if (isDefered)
		{
			return context.Interaction.ModifyOriginalResponseAsync(x =>
				{
					x.Embed = em;
					x.AllowedMentions = AllowedMentions.None;
				}
			);
		}

		return context.Interaction.RespondAsync(embed: em,
			allowedMentions: AllowedMentions.None,
			ephemeral: ephemeral
		);
	}
}