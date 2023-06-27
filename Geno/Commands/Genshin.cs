using System.Text;
using Database;
using Database.Models;
using Discord;
using Discord.Interactions;
using EnkaAPI;
using Geno.Responsers.Success.Modules;
using Geno.Utils.Extensions;
using Geno.Utils.Types;
using HoYoLabApi.Enums;
using HoYoLabApi.GenshinImpact;
using HoYoLabApi.Models;
using HoYoLabApi.Static;
using Extensions = Geno.Utils.Extensions.Extensions;

namespace Geno.Commands;

//[Group("genshin", "Genshin Impact commands")]
public class Genshin : ModuleBase
{
	private const string m_baseLink = "https://genshin.hoyoverse.com/en/gift?code=";
	private readonly DatabaseProvider m_databaseProvider;
	private readonly EnkaApiClient m_enkaApiClient;
	private readonly GenshinImpactService m_genshinImpactService;

	public Genshin(DatabaseProvider databaseProvider,
		EnkaApiClient enkaApiClient,
		GenshinImpactService genshinImpactService)
	{
		m_databaseProvider = databaseProvider;
		m_enkaApiClient = enkaApiClient;
		m_genshinImpactService = genshinImpactService;
	}

	[SlashCommand("dailies", "enable/disable dailies")]
	public async Task AutoDailiesSwitch(bool isEnable)
	{
		await DeferAsync(true);
		var profile = await m_databaseProvider.GetUser(Context.User.Id);
		if (profile.HoYoLabCookies == string.Empty)
		{
			var (embed, components) = EmbedExtensions.GetRegistrationForm(Context.User.Id);
			await Respond(embed, components: components, ephemeral: true, isDefered: true, isFolluwup: false);
			return;
		}

		var dailies = profile.EnabledAutoDailies;
		dailies.Genshin = isEnable;

		profile.EnabledAutoDailies = dailies;
		await m_databaseProvider.SetUser(profile);
		await Respond(new EmbedBuilder().WithDescription("Genshin auto-claim set to" + isEnable), isDefered: true);
	}

	[ComponentInteraction("genshin_auto_claim_codes_*", true)]
	public async Task ClaimCodes(string codesString)
	{
		await DeferAsync(true);
		var profile = await m_databaseProvider.GetUser(Context.User.Id);
		if (profile.HoYoLabCookies == string.Empty)
		{
			var (embed, components) = EmbedExtensions.GetRegistrationForm(Context.User.Id);
			await Respond(embed, components: components, ephemeral: true, isDefered: true, isFolluwup: true);
			return;
		}

		var codes = codesString.Split(',');
		var max = codes.Length;
		var counter = 1;
		await foreach (var res in m_genshinImpactService.CodesClaimAsync(codes, profile.HoYoLabCookies, Region.Europe))
		{
			await Respond(
				new EmbedBuilder().WithDescription(
					$"`{counter.ToString()}`/`{max.ToString()}`*({((int)((float)counter / max * 100)).ToString()})*\n[`{codes[counter - 1]}`]: {res.Message}"),
				ephemeral: true, isFolluwup: true);
			counter++;
		}
	}

	[UserCommand("Genshin profile")]
	public async Task GenshinProfile(IUser user)
	{
		await DeferAsync();
		var profile = await m_databaseProvider.GetUser(user.Id);
		if (profile.HoYoLabCookies == string.Empty)
		{
			var (embed, components) = EmbedExtensions.GetRegistrationForm(user.Id);
			await Respond(embed, components: components, ephemeral: false, isDefered: true);
			return;
		}

		var cookies = profile.HoYoLabCookies.ParseCookies();
		var acc = await m_genshinImpactService.GetGameAccountAsync(cookies);

		await Respond(new EmbedBuilder().WithDescription("Found:"), ephemeral: true, isDefered: true);
		foreach (var data in acc?.Data?.GameAccounts ?? Array.Empty<GameData>())
		{
			var info = await m_enkaApiClient.GetInfo(data.Uid);
			await Respond(Context.Profile(info), isDefered: true, isFolluwup: true);
		}
	}

	[MessageCommand("Make Genshin codes link")]
	public async Task MakeCodeLinks(IMessage message)
	{
		await DeferAsync();

		var content = message.Content!;
		var codes = Extensions.CodeRegex.Matches(content).Select(x => x.Value).ToArray();

		CreateLinks(codes, out var links);
		var components = new ComponentBuilder();
		var description = new StringBuilder("Кодеки:\n");

		components
			.AddRow(new ActionRowBuilder()
				.WithButton(new ButtonBuilder()
					.WithCustomId("genshin_auto_claim_codes_" + string.Join(',', codes))
					.WithLabel("Auto-claim")
					.WithStyle(ButtonStyle.Primary)
				)
			);

		for (byte i = 0; i < links.Length; i++)
		{
			description.AppendFormat("[{0}]({1})\n", codes[i], links[i]);
			components.AddRow(new ActionRowBuilder()
				.WithButton(codes[i], style: ButtonStyle.Link, url: links[i]));
		}

		await Respond(new EmbedBuilder().WithDescription(description.ToString()), components: components,
			isDefered: true);
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
		await Respond(new EmbedBuilder().WithDescription("Done"), ephemeral: true);
	}

	private async ValueTask<GuildDocument> UpdateDoc(IMessage message, string userId)
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