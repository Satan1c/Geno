using System.Text;
using Database;
using Database.Models;
using Discord;
using Discord.Interactions;
using Geno.Handlers;
using Geno.Responsers;
using Geno.Utils.Extensions;
using Geno.Utils.StaticData;
using Geno.Utils.Types;
using HoYoLabApi;
using HoYoLabApi.Classes;
using HoYoLabApi.Static;

namespace Geno.Commands;

[Group("settings", "settings commands group")]
[EnabledInDm(false)]
public class Settings : ModuleBase
{
	private static DatabaseProvider s_databaseProvider = null!;
	private static IHoYoLabClient m_hoYoLabClient;
	private static AccountSearcher s_accountSearcher = null!;

	public Settings(DatabaseProvider databaseProvider, IHoYoLabClient hoYoLabClient)
	{
		m_hoYoLabClient = hoYoLabClient;
		s_accountSearcher = new AccountSearcher(hoYoLabClient);
		s_databaseProvider = databaseProvider;
		m_hoYoLabClient = hoYoLabClient;
	}

	[ComponentInteraction("hoyo_registration_button", true)]
	public async Task R()
	{
		await RespondWithModalAsync<RegisterModal>("hoyo_registration_modal");
	}
	
	[ModalInteraction("hoyo_registration_modal", true)]
	public async Task M(RegisterModal modal)
	{
		await Respond(new EmbedBuilder().WithDescription("Registering..."), ephemeral: true);
		try
		{
			var cookies = modal.Cookies.ParseCookies();
			//var data = await s_accountSearcher.GetGameAccountAsync(modal.Cookies);
			var data = await m_hoYoLabClient
				.GetGamesArrayAsync(new Request(
					"api-account-os",
					"account/binding/api/getUserGameRolesByCookieToken",
					cookies,
					new Dictionary<string, string>()
					{
						{
							"uid",
							cookies.AccountId.ToString()
						},
						{
							"sLangKey",
							cookies.Language.GetLanguageString()
						}
					}));
			if (data.Code == -100)
			{
				await Respond(new EmbedBuilder().WithColor(Color.Red).WithDescription("Invalid cookies"),
					ephemeral: true);
				return;
			}
			else if (data.Code != 0 || data.Data.GameAccounts.Length < 1)
			{
				await ClientEvents.OnLog(new LogMessage(LogSeverity.Error, nameof(M), $"Invalid cookies, if check, code: {data.Code.ToString()} message: {data.Message}"));
				
				await Respond(new EmbedBuilder().WithColor(Color.Red).WithDescription("Invalid cookies"),
					ephemeral: true);
				return;
			}
		}
		catch
		{
			await ClientEvents.OnLog(new LogMessage(LogSeverity.Error, nameof(M), "Invalid cookies, catch block"));
			await Respond(new EmbedBuilder().WithColor(Color.Red).WithDescription("Invalid cookies"), ephemeral: true);
			return;
		}
		
		await ClientEvents.OnLog(new LogMessage(LogSeverity.Verbose, nameof(M), "Valid cookies"));
		
		var profile = await s_databaseProvider.GetUser(Context.User.Id);
		profile.HoYoLabCookies = modal.Cookies;
		await s_databaseProvider.SetUser(profile);
		await Respond(new EmbedBuilder().WithDescription("Cookies registered"), isDefered: true, ephemeral: true);
	}

	private static string GetMessage(ref GuildDocument config)
	{
		var message = new StringBuilder("Rank roles config:\n");
		foreach (var (k, v) in config.RankRoles)
		{
			//var mentions = new RefList<string>(v.Length);
			var mentions = new StringBuilder(v.Length * 20);
			foreach (var id in v)
				mentions.Append($"<@&{id.ToString()}>").Append(',');

			mentions.Remove(mentions.Length - 1, 1);
			message.Append($"`{k}` - ")
				.Append(mentions)
				.Append('\n');
		}

		return message.ToString();
	}

	[Group("genshin", "Genshin Impact functional settings")]
	public class Genshin : ModuleBase
	{
		[MessageCommand("Set rank-roles")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		[RequireBotPermission(GuildPermission.ManageRoles)]
		public async Task SetRankRoles(IMessage message)
		{
			//await Context.Interaction.DeferAsync();
			var pairs = message.Content.Split('\n');
			var cfg = await s_databaseProvider.GetConfig(Context.Guild.Id);
			cfg.RankRoles = new Dictionary<string, ulong[]>();

			for (byte i = 0; i < pairs.Length; i++)
			{
				var pair = pairs[i].Split('-');
				var k = byte.Parse(pair[0]);
				var v = pair[1].Split(',').Select(s => ulong.Parse(s)).ToArray();

				cfg.RankRoles[k.ToString()] = v;
			}

			await s_databaseProvider.SetConfig(cfg);
			await Respond(new EmbedBuilder().WithDescription("Done"), ephemeral: true);
		}

		[SlashCommand("set_rank", "set rank for user")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		[RequireBotPermission(GuildPermission.ManageRoles)]
		public async Task Setup(
			[Summary("user", "user to set rank for")]
			IUser user,
			[MinValue(1)] [MaxValue(60)] [Summary("rank", "rank to set")]
			byte rank)
		{
			var member = Context.Guild.GetUser(user.Id)!;
			var config = await s_databaseProvider.GetConfig(Context.Guild.Id);

			await member.UpdateRoles(rank, config);
			await Respond(new EmbedBuilder().WithDescription("Done"), ephemeral: true);
		}

		[SlashCommand("get_ranks", "show current ranks config")]
		[RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task GetConfig()
		{
			var config = await s_databaseProvider.GetConfig(Context.Guild.Id);

			await Respond(new EmbedBuilder().WithDescription(GetMessage(ref config)),
				ephemeral: true);
		}
	}

	[Group("voice_rooms", "Voice rooms settings")]
	public class VoiceRooms : ModuleBase
	{
		[SlashCommand("set_name",
			"Voice room name template; {Count},{DisplayName},{Username},{UserTag},{ActivityName}")]
		[RequireBotPermission(BotPermissions.UtilsAddVoice)]
		[RequireUserPermission(UserPermissions.UtilsAddVoice)]
		public async Task SetVoiceChannelNames(
			IVoiceChannel channel,
			[MaxLength(50)] [Summary("template", "created channel name, default - Party #{Count}")]
			string name = "Default - Party #{Count}")
		{
			if (await channel.GetCategoryAsync() is null)
				throw new Exception("Voice channel must have a category");

			var config = await s_databaseProvider.GetConfig(Context.Guild.Id);
			config.VoicesNames[channel.Id.ToString()] = name;

			await s_databaseProvider.SetConfig(config);
			await Respond(new EmbedBuilder().WithDescription("Done"), ephemeral: true);
		}

		[SlashCommand("add_creator", "Add creator of voice room")]
		[RequireBotPermission(BotPermissions.UtilsAddVoice)]
		[RequireUserPermission(UserPermissions.UtilsAddVoice)]
		public async Task AddCreator(
			[Summary("", "")] IVoiceChannel channel,
			[MinLength(1)] [MaxLength(50)] [Summary("template", "created channel name, default - Party #{Count}")]
			string name)
		{
			if (await channel.GetCategoryAsync() is not { } category)
				throw new Exception("Voice channel must have a category");

			var config = await s_databaseProvider.GetConfig(Context.Guild.Id);
			config.Channels[channel.Id.ToString()] = category.Id;
			config.VoicesNames[channel.Id.ToString()] = name;

			await s_databaseProvider.SetConfig(config);
			await Respond(new EmbedBuilder().WithDescription("Done"),
				ephemeral: true);
		}

		[SlashCommand("remove_creator", "Remove creator of voice room")]
		[RequireUserPermission(UserPermissions.UtilsAddVoice)]
		public async Task RemoveCreator(
			[Summary("", "")] IVoiceChannel channel)
		{
			if (await channel.GetCategoryAsync() is null)
				throw new Exception("Voice channel must have a category");

			var config = await s_databaseProvider.GetConfig(Context.Guild.Id);
			config.Channels.Remove(channel.Id.ToString());
			config.VoicesNames.Remove(channel.Id.ToString());

			await s_databaseProvider.SetConfig(config);
			await Respond(new EmbedBuilder().WithDescription("Done"),
				ephemeral: true);
		}

		[SlashCommand("get_creators", "Get creators of voice rooms")]
		[RequireUserPermission(UserPermissions.UtilsAddVoice)]
		public async Task GetCreators()
		{
			var config = await s_databaseProvider.GetConfig(Context.Guild.Id);
			if (config.Channels.Count < 1)
				throw new Exception("No creators was found");

			var txt = new StringBuilder();
			foreach (var (k, _) in config.Channels)
				txt.Append($"<#{k}>\n");

			await Respond(new EmbedBuilder().WithDescription(txt.ToString()),
				ephemeral: true);
		}
	}
}