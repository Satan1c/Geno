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
				.AddField("Уровень буста", $"`{guild.Boost.BoostLevelToString()}`")
				.AddField("Есть ли бот на сервере", $"`{(guild.IsBotOnServer ? "Да" : "Нет")}`"),
			_ => new EmbedBuilder()
				.WithAuthor(guild.Name, guild.Avatar, guild.Url)
				.WithDescription($"`{string.Join("`, `", guild.Tags.Select(x => x.TagsToString()))}`")
				.AddField("Members", $"`{guild.Members.ToString()}`")
				.AddField("Online", $"`{guild.Online.ToString()}`")
				.AddField("Badge", $"`{guild.Badges.BadgeToString()}`")
				.AddField("Boost", $"`{guild.Boost.BoostLevelToString()}`")
				.AddField("Is bot on server", $"`{(guild.IsBotOnServer ? "Yes" : "No")}`")
		};

		await context.Respond(embed);
	}

	public static async Task WarnsInfo(this ShardedInteractionContext context, UserWarns warns)
	{
		var embed = context.GetLocale() switch
		{
			_ => new EmbedBuilder()
				.WithAuthor(warns.User?.Username, warns.User?.GetAvatarUrl(size: 512))
				.WithDescription(warns.Warns.ToString())
		};

		await context.Respond(embed);
	}

	public static async Task GuildRatesInfo(this ShardedInteractionContext context,
		Task<Guild> guildTask,
		Task<Dictionary<User, Rate>> ratesTask)
	{
		await context.Interaction.DeferAsync();

		var guild = await guildTask;
		
		var embed = context.GetLocale() switch
		{
			_ => new EmbedBuilder()
				.WithAuthor(guild.Name, guild.Avatar, guild.Url)
		};

		var rates = await ratesTask;

		foreach (var (k, v) in rates)
		{
			var user = k.Instance;
			embed.AddField($"`{user?.Username ?? "unknown"}`#`{user?.Discriminator ?? "unknown"}`", v.RateToString());
		}

		await context.Respond(embed, false, true);
	}
}