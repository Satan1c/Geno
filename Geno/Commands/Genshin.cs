using Database;
using Database.Models;
using Discord;
using Discord.Interactions;
using EnkaAPI;
using Geno.Responsers.Success.Modules;
using Geno.Utils.Extensions;
using Localization;
using Localization.Models;

namespace Geno.Commands;

[Group("genshin", "Genshin Impact commands")]
public class Genshin : InteractionModuleBase<ShardedInteractionContext>
{
	private const string m_baseLink = "https://genshin.hoyoverse.com/en/gift?code=";
	private readonly DatabaseProvider m_databaseProvider;
	private readonly EnkaApiClient m_enkaApiClient;

	public Genshin(DatabaseProvider databaseProvider,
		EnkaApiClient enkaApiClient)
	{
		m_databaseProvider = databaseProvider;
		m_enkaApiClient = enkaApiClient;
	}

	[UserCommand("Genshin profile")]
	public async Task GenshinProfile(IUser user)
	{
		const uint uid = 700289769;

		var data = await m_enkaApiClient.GetInfo(uid);
		await Context.Profile(data);
	}

	[MessageCommand("Make code links")]
	public async Task MakeCodeLinks(IMessage message)
	{
		await DeferAsync();

		var content = message.Content!;
		var codes = content.Split('\n');

		CreateLinks(codes, out var links);
		var components = new ComponentBuilder();

		for (byte i = 0; i < links.Length; i++)
			components.AddRow(new ActionRowBuilder()
				.WithButton(codes[i], style: ButtonStyle.Link, url: links[i]));

		await ModifyOriginalResponseAsync(x =>
		{
			x.Embed = new EmbedBuilder().WithDescription("Кодеки:").Build();
			x.Components = components.Build();
		});
	}

	[MessageCommand("Rank info")]
	[RequireBotPermission(GuildPermission.ManageRoles)]
	[EnabledInDm(false)]
	public async Task RankInfo(IMessage message)
	{
		if (!uint.TryParse(message.Content, out var uid))
			//return new Result(false, new EmbedBuilder().);
			throw new Exception("Invalid AR value");
		if (message.Author.Id != Context.User.Id)
			throw new Exception("That isn't your message");

		var data = (await m_enkaApiClient.GetInfo(uid)).PlayerInfo;
		var doc = await UpdateDoc(message, Context.User.Id.ToString());

		await Context.Guild.GetUser(message.Author.Id).UpdateRoles(data.AdventureRank, doc);
		await RespondAsync("Done", ephemeral: true);
	}

	private async Task<GuildDocument> UpdateDoc(IMessage message, string userId)
	{
		var doc = await m_databaseProvider.GetConfig(Context.Guild.Id);
		if (doc.UserScreens.TryGetValue(userId, out var value) && value != message.Id)
			await message.Channel.DeleteMessageAsync(doc.UserScreens[userId]);

		doc.UserScreens[userId] = message.Id;
		await m_databaseProvider.SetConfig(doc);

		return doc;
	}

	//private static void CreateLinks(in string[] codes, out string[] links, out string[] contents)
	private static void CreateLinks(in string[] codes, out string[] links)
	{
		var linksRaw = new LinkedList<string>();
		var contentsRaw = new LinkedList<string>();

		foreach (var c in codes)
		{
			var link = m_baseLink + c;

			linksRaw.AddLast(link);
			contentsRaw.AddLast($"[{c}]({link})");
		}

		links = linksRaw.ToArray();
		//contents = contentsRaw.ToArray();
	}
}