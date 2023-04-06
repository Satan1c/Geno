using System.Text;
using Database;
using Database.Models;
using Discord;
using Discord.Interactions;
using Geno.Responsers.Success;
using Geno.Utils.Extensions;
using Geno.Utils.StaticData;

namespace Geno.Commands;

[Group("settings", "settings commands group")]
[EnabledInDm(false)]
public class Settings : InteractionModuleBase<ShardedInteractionContext>
{
	private static DatabaseProvider s_databaseProvider = null!;

	public Settings(DatabaseProvider databaseProvider)
	{
		s_databaseProvider = databaseProvider;
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
	public class Genshin : InteractionModuleBase<ShardedInteractionContext>
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
			await RespondAsync("Done", ephemeral: true);
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
			await RespondAsync("Done", ephemeral: true);
		}

		[SlashCommand("get_ranks", "show current ranks config")]
		[RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task GetConfig()
		{
			var config = await s_databaseProvider.GetConfig(Context.Guild.Id);

			/*if (config.RankRoles.Count == 0)
			{
				await RespondAsync(
					"message.ToString()",
					ephemeral: true,
					allowedMentions: AllowedMentions.None);
				return;
			}*/

			await RespondAsync(
				GetMessage(ref config),
				ephemeral: true,
				allowedMentions: AllowedMentions.None);
		}
	}

	[Group("voice_rooms", "Voice rooms settings")]
	public class VoiceRooms : InteractionModuleBase<ShardedInteractionContext>
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
			{
				await Context.Respond(new EmbedBuilder().WithDescription("Voice channel must have a category"), true);
				return;
			}

			var config = await s_databaseProvider.GetConfig(Context.Guild.Id);
			config.VoicesNames[channel.Id.ToString()] = name;

			await s_databaseProvider.SetConfig(config);
			await Context.Respond(new EmbedBuilder().WithDescription("Done"), true);
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
			await RespondAsync("Done",
				allowedMentions: AllowedMentions.None,
				ephemeral: true);
		}

		[SlashCommand("remove_creator", "Remove creator of voice room")]
		[RequireUserPermission(UserPermissions.UtilsAddVoice)]
		public async Task RemoveCreator(
			[Summary("", "")] IVoiceChannel channel)
		{
			if (await channel.GetCategoryAsync() is null)
			{
				await RespondAsync("Voice channel must have a category",
					allowedMentions: AllowedMentions.None,
					ephemeral: true);
				return;
			}

			var config = await s_databaseProvider.GetConfig(Context.Guild.Id);
			config.Channels.Remove(channel.Id.ToString());
			config.VoicesNames.Remove(channel.Id.ToString());

			await s_databaseProvider.SetConfig(config);
			await RespondAsync("Done",
				allowedMentions: AllowedMentions.None,
				ephemeral: true);
		}

		[SlashCommand("get_creators", "Get creators of voice rooms")]
		[RequireUserPermission(UserPermissions.UtilsAddVoice)]
		public async Task GetCreators()
		{
			var config = await s_databaseProvider.GetConfig(Context.Guild.Id);
			if (config.Channels.Count < 1)
			{
				await RespondAsync("None",
					allowedMentions: AllowedMentions.None,
					ephemeral: true);
				return;
			}

			var txt = new StringBuilder();
			foreach (var (k, _) in config.Channels)
				txt.Append($"<#{k}>\n");

			await RespondAsync(txt.ToString(),
				allowedMentions: AllowedMentions.None,
				ephemeral: true);
		}
	}
}