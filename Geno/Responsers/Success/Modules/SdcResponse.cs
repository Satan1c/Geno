using System.Text;
using Discord;
using Discord.Interactions;
using Geno.Utils.Extensions;
using Geno.Utils.Types;
using Localization;
using SDC_Sharp.DiscordNet.Types;
using SDC_Sharp.Types;
using SDC_Sharp.Types.Enums;
using Category = Localization.Models.Category;

namespace Geno.Responsers.Success.Modules;

public static class SdcResponse
{
	private static Category s_category;

	public static void Init(LocalizationManager localizationManager)
	{
		s_category = localizationManager.GetCategory("genshin");
	}
	
	public static ValueTask GuildInfo(this ShardedInteractionContext context, Guild guild)
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

		return context.Respond(embed);
	}

	public static ValueTask WarnsInfo(this ShardedInteractionContext context, UserWarns warns)
	{
		var embed = context.GetLocale() switch
		{
			_ => new EmbedBuilder()
				.WithAuthor(warns.User?.Username, warns.User?.GetAvatarUrl(size: 512))
				.WithDescription(warns.Warns.ToString())
		};

		return context.Respond(embed);
	}

	public static async ValueTask GuildRatesInfo(this ShardedInteractionContext context,
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
			embed.AddField(new StringBuilder()
					.AppendFormat("`{0}`#`{1}`", user?.Username ?? "unknown", user?.Discriminator ?? "unknown").ToString(),
				v.RateToString());
		}

		await context.Respond(embed, false, true).ConfigureAwait(false);
	}
}