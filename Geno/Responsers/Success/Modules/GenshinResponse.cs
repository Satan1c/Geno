using Discord;
using EnkaAPI.Types;
using EnkaAPI.Types.PlayerInfo;
using Geno.Utils.Extensions;
using Localization;
using Localization.Models;

namespace Geno.Responsers.Success.Modules;

public static class GenshinResponse
{
	private static Category s_category;

	public static void Init(LocalizationManager localizationManager)
	{
		s_category = localizationManager.GetCategory("genshin");
	}

	public static EmbedBuilder Profile(this IInteractionContext context, Info info)
	{
		var player = info.PlayerInfo;
		var avatars = info.PlayerInfo.AvatarInfoList?.ToArray() ?? Array.Empty<ShortAvatarInfo>();
		var locals = s_category.GetDataFor("profile").GetForLocale(context);

		var abyssTitle = locals["abyss_title"];
		var abyssValue = locals["abyss_value"].FormatWith(player);

		var arTitle = locals["ar_title"];
		var arValue = locals["ar_value"].FormatWith(player);

		var embed = new EmbedBuilder()
			.WithAuthor(player.Nickname)
			.WithDescription(player.Description)
			.AddField(abyssTitle, abyssValue, true)
			.AddField(arTitle, arValue, true)
			.AddEmpty();

		foreach (var avatar in avatars)
			embed.AddField(avatar.AvatarId.ToString(),
				$"`{avatar.Level.ToString()}`/`90`", true);

		return embed;
	}
}