using Discord;
using Discord.Interactions;
using Geno.Errors;
using Geno.Utils;
using SDC_Sharp.DiscordNet.Types;
using SDC_Sharp.Types;
using SDC_Sharp.Types.Enums;

namespace Geno.Responses;

public static class SdcResponse
{
	private static async Task Resolve(this ShardedInteractionContext context, EmbedBuilder embed,
		bool ephemeral = false)
	{
		await context.Interaction.RespondAsync(embed: embed.Build(),
			allowedMentions: AllowedMentions.None,
			ephemeral: ephemeral
		);
	}

	public static async Task GuildInfo(this ShardedInteractionContext context, Guild guild)
	{
		var embed = context.GetLocale() switch
		{
			UserLocales.Russian => new EmbedBuilder()
				.WithAuthor(guild.Name, guild.Avatar, guild.Url)
				.WithDescription($"`{string.Join("`, `", guild.Tags.Select(x => x.TagsToString()))}`")
				.AddField("Участников", $"`{guild.Members.ToString()}`")
				.AddField("Онлайн", $"`{guild.Online.ToString()}`")
				.AddField("Значки", $"`{guild.Badges.BadgeToString()}`")
				.AddField("Уровень буста", $"`{guild.Boost.BoostLevelToString()}`"),
			_ => new EmbedBuilder()
				.WithAuthor(guild.Name, guild.Avatar, guild.Url)
				.WithDescription($"`{string.Join("`, `", guild.Tags.Select(x => x.TagsToString()))}`")
				.AddField("Members", $"`{guild.Members.ToString()}`")
				.AddField("Online", $"`{guild.Online.ToString()}`")
				.AddField("Badge", $"`{guild.Badges.BadgeToString()}`")
				.AddField("Boost", $"`{guild.Boost.BoostLevelToString()}`")
		};

		await context.Resolve(embed);
	}

	public static async Task WarnsInfo(this ShardedInteractionContext context, UserWarns warns)
	{
		var embed = context.GetLocale() switch
		{
			_ => new EmbedBuilder()
				.WithAuthor(warns.User?.Username, warns.User?.GetAvatarUrl(size: 512))
				.WithDescription(warns.Warns.ToString())
		};

		await context.Resolve(embed);
	}

	public static async Task GuildRatesInfo(this ShardedInteractionContext context, Guild guild,
		IDictionary<User, Rate> rates)
	{
		var embed = context.GetLocale() switch
		{
			_ => new EmbedBuilder()
				.WithAuthor(guild.Name, guild.Avatar, guild.Url)
		};

		foreach (var (k, v) in rates)
		{
			var user = k.Instance!;
			embed.AddField($"`{user.Username}`#`{user.Discriminator}`", v.RateToString());
		}

		await context.Resolve(embed);
	}
}