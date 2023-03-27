using Discord;
using EnkaAPI.Types;
using Geno.Utils.Extensions;
using Localization.Models;

namespace Geno.Responsers.Success.Modules;

public static class GenshinResponse
{
	public static Task Profile(this IInteractionContext context, Info info, Category localizations)
	{
		var player = info.PlayerInfo;
		var avatars = info.PlayerInfo.AvatarInfoList.ToArray();
		var locals = localizations.GetDataFor("profile").GetForLocale(context);

		var abyssTitle = locals["abyss_title"];
		var abyssValue = locals["abyss_value"].FormatWith(player);
		
		var arTitle = locals["ar_title"];
		var arValue = locals["ar_value"].FormatWith(player);
		
		var embed = new EmbedBuilder()
			.WithAuthor(player.Nickname)
			.WithDescription(player.Description)
			.AddField(abyssTitle, abyssValue, true)
			.AddField(arTitle, arValue, true);

		foreach (var avatar in avatars)
			embed.AddField(avatar.AvatarId.ToString(),
				$"`{avatar.Level.ToString()}`/`90`", true);

		return context.Respond(embed, true);
	}
}