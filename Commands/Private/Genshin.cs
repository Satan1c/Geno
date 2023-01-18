using System.Text;
using Discord;
using Discord.Extensions;
using Discord.Interactions;
using Discord.WebSocket;
using EnkaAPI;
using Geno.Database;
using Geno.Utils;

namespace Geno.Commands.Private;

[Group("genshin", "genshin commands")]
[Private(Category.Genshin)]
public class Genshin : InteractionModuleBase<ShardedInteractionContext>
{
	private readonly DatabaseProvider m_databaseProvider;
	private readonly EnkaApiClient m_enkaApiClient;
	
	private const string m_baseLink = "https://genshin.hoyoverse.com/en/gift?code=";

	public Genshin(DatabaseProvider databaseProvider, EnkaApiClient enkaApiClient)
	{
		m_databaseProvider = databaseProvider;
		m_enkaApiClient = enkaApiClient;
	}

	[MessageCommand("Make code links")]
	public async Task MakeCodeLinks(IMessage message)
	{
		await DeferAsync();
		
		var codes = message.Content.Split('\n');
		CreateLinks(codes, out var links, out _);

		var components = new ComponentBuilder();
		
		for (byte i = 0; i < links.Length; i++)
			components
				.AddRow(
					new ActionRowBuilder()
						.WithButton(codes[i], style: ButtonStyle.Link, url: links[i]));

		await ModifyOriginalResponseAsync(x =>
		{
			x.Embed = new EmbedBuilder().WithDescription("Кодеки:").Build();
			x.Components = components.Build();
		});
	}

	private static void CreateLinks(in string[] codes, out string[] links, out string[] contents)
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
		contents = contentsRaw.ToArray();
	}

	[MessageCommand("Rank info")]
	public async Task RankInfo(IMessage message)
	{
		if (!uint.TryParse(message.Content, out var uid))
			throw new Exception("Invalid AR value");
		if (message.Author.Id != Context.User.Id)
			throw new Exception("That isn't your message");

		var data = (await m_enkaApiClient.GetData(uid)).PlayerInfo;
		var doc = await UpdateDoc(message, Context.User.Id.ToString());

		await UpdateRoles(data.AdventureRank, doc, Context.Guild.GetUser(message.Author.Id));
		await RespondAsync("Done", ephemeral: true);
	}

	[MessageCommand("Set ranks roles")]
	[RequireUserPermission(GuildPermission.ManageRoles)]
	[RequireBotPermission(GuildPermission.ManageRoles)]
	public async Task SetRankRoles(IMessage message)
	{
		//await Context.Interaction.DeferAsync();
		var pairs = message.Content.Split('\n');
		var cfg = await m_databaseProvider.GetConfig(Context.Guild.Id);
		cfg.RankRoles = new Dictionary<string, ulong[]>();

		string[] pair;
		for (byte i = 0; i < pairs.Length; i++)
		{
			pair = pairs[i].Split('-');
			var k = byte.Parse(pair[0]);
			var v = pair[1].Split(',').Select(x => ulong.Parse(x)).ToArray();
			cfg.RankRoles[k.ToString()] = v;
		}

		await m_databaseProvider.SetConfig(cfg);
		await RespondAsync("Done", ephemeral: true);
	}

	[SlashCommand("set_rank", "set rank for user")]
	[RequireUserPermission(GuildPermission.ManageRoles)]
	[RequireBotPermission(GuildPermission.ManageRoles)]
	public async Task Setup(IUser user, byte newRank)
	{
		var member = Context.Guild.GetUser(user.Id)!;
		var cfg = await m_databaseProvider.GetConfig(Context.Guild.Id);
		var role = cfg.RankRoles.GetPerfectRole(newRank.ToString());
		var remove = member.Roles
			.Where(x => cfg.RankRoles.Values.Any(y => y.Contains(x.Id)))
			.Where(x => !role.Contains(x.Id))
			.Select(x => x.Id)
			.ToArray();

		if (remove.Any())
			await member.RemoveRolesAsync(remove);

		await member.AddRolesAsync(role);

		await RespondAsync("Done", ephemeral: true);
	}

	[SlashCommand("get_ranks", "show current ranks config")]
	[RequireUserPermission(GuildPermission.ManageGuild)]
	public async Task GetConfig()
	{
		var config = await m_databaseProvider.GetConfig(Context.Guild.Id);
		var message = new StringBuilder("Rank roles config:\n");
		foreach (var (k, v) in config.RankRoles)
		{
			message.Append($"`{k}`");
			message.Append(" - ");
			message.Append(string.Join(',', v.Select(x => $"<@&{x}>")));
			message.Append('\n');
		}

		await RespondAsync(
			message.ToString(),
			ephemeral: true,
			allowedMentions: AllowedMentions.None);
	}

	private static async Task UpdateRoles(uint adventureRank, GuildDocument doc, SocketGuildUser member)
	{
		var role = doc.RankRoles.GetPerfectRole(adventureRank.ToString());
		var remove = member.Roles
			.Where(x => doc.RankRoles.Values.Any(y => y.Contains(x.Id)))
			.Where(x => !role.Contains(x.Id))
			.Select(x => x.Id)
			.ToArray();

		if (remove.Any())
			await member.RemoveRolesAsync(remove);

		await member.AddRolesAsync(role);
	}

	private async Task<GuildDocument> UpdateDoc(IMessage message, string userId)
	{
		var doc = await m_databaseProvider.GetConfig(Context.Guild.Id);
		if (doc.UserScreens.ContainsKey(userId) && doc.UserScreens[userId] != message.Id)
			await message.Channel.DeleteMessageAsync(doc.UserScreens[userId]);

		doc.UserScreens[userId] = message.Id;
		await m_databaseProvider.SetConfig(doc);

		return doc;
	}
}