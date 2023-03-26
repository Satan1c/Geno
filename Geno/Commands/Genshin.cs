using Discord;
using Discord.Interactions;
using EnkaAPI;
using Geno.Responsers.Success;

namespace Geno.Commands;

[Group("genshin", "Genshin Impact commands")]
public class Genshin : InteractionModuleBase<ShardedInteractionContext>
{
	private readonly EnkaApiClient m_enkaApiClient;

	public Genshin(EnkaApiClient enkaApiClient)
	{
		m_enkaApiClient = enkaApiClient;
	}

	[UserCommand("Genshin profile")]
	public async Task GenshinProfile(IUser user)
	{
		const uint uid = 700289769;

		var data = await m_enkaApiClient.GetInfo(uid);
		var player = data.PlayerInfo;
		var avatars = data.PlayerInfo.AvatarInfoList.ToArray();

		var embed = new EmbedBuilder()
			.WithAuthor(player.Nickname)
			.WithDescription(player.Description);

		foreach (var avatar in avatars)
			embed.AddField(avatar.AvatarId.ToString(),
				$"`{avatar.Level.ToString()}`/`90`", true);

		await Context.Respond(embed, true);
	}
}