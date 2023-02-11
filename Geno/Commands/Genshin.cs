using Discord;
using Discord.Interactions;
using EnkaAPI;
using Geno.Responses;

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

		var data = await m_enkaApiClient.GetData(uid);
		var player = data.PlayerInfo;
		var avatars = data.AvatarInfoList.ToArray();

		var embed = new EmbedBuilder()
			.WithAuthor(player.Nickname)
			.WithDescription(player.Description);

		foreach (var avatar in avatars)
		{
			var props = avatar.Props;
			var level = props.Level.Value;
			var ascension = props.Ascension.Value;

			embed.AddField(avatar.AvatarId.ToString(),
				$"`{level.ToString()}`/`90` `{ascension.ToString()}`/`6`", true);
		}

		await Context.Respond(embed, true);
	}
}