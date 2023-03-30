﻿using System.Text;
using Database;
using Discord;
using Discord.Interactions;
using Geno.Responsers.Success;
using Geno.Utils.Extensions;
using Geno.Utils.StaticData;
using Geno.Utils.Types;

namespace Geno.Commands;

[Group("settings", "settings commands group")]
[EnabledInDm(false)]
public class Settings : InteractionModuleBase<ShardedInteractionContext>
{
	private static DatabaseProvider m_databaseProvider;

	public Settings(DatabaseProvider databaseProvider)
	{
		m_databaseProvider = databaseProvider;
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
			var cfg = await m_databaseProvider.GetConfig(Context.Guild.Id);
			cfg.RankRoles = new Dictionary<string, ulong[]>();

			for (byte i = 0; i < pairs.Length; i++)
			{
				var pair = pairs[i].Split('-');
				var k = byte.Parse(pair[0]);
				var v = pair[1].Split(',').Select(s => ulong.Parse(s)).ToArray();
			
				cfg.RankRoles[k.ToString()] = v;
			}

			await m_databaseProvider.SetConfig(cfg);
			await RespondAsync("Done", ephemeral: true);
		}
		
		[SlashCommand("set_rank", "set rank for user")]
		[RequireUserPermission(GuildPermission.ManageRoles)]
		[RequireBotPermission(GuildPermission.ManageRoles)]
		public async Task Setup(
			[Summary("user", "user to set rank for")]
			IUser user,
			[MinValue(1)] [MaxValue(60)]
			[Summary("rank", "rank to set")]
			byte rank)
		{
			var member = Context.Guild.GetUser(user.Id)!;
			var config = await m_databaseProvider.GetConfig(Context.Guild.Id);
			var role = config.RankRoles.GetPerfectRole(rank.ToString());
			var remove = member.Roles
				.Where(x => config.RankRoles.Values.Any(y => y.Contains(x.Id)))
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

			/*if (config.RankRoles.Count == 0)
			{
				await RespondAsync(
					"message.ToString()",
					ephemeral: true,
					allowedMentions: AllowedMentions.None);
				return;
			}*/
			
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
	}

	[Group("voice_rooms", "Voice rooms settings")]
	public class VoiceRooms : InteractionModuleBase<ShardedInteractionContext>
	{
		[SlashCommand("set_name", "Voice room name template; {Count},{DisplayName},{Username},{UserTag},{ActivityName}")]
		[RequireBotPermission(BotPermissions.UtilsAddVoice)]
		[RequireUserPermission(UserPermissions.UtilsAddVoice)]
		public async Task SetVoiceChannelNames(
			IVoiceChannel channel,
			[MinLength(1), MaxLength(50), Summary("template", "created channel name, default - Party #{Count}")]
			string name = "Default - Party #{Count}")
		{
			if (await channel.GetCategoryAsync() is null)
			{
				await Context.Respond(new EmbedBuilder().WithDescription("Voice channel must have a category"), true);
				return;
			}

			var config = await m_databaseProvider.GetConfig(Context.Guild.Id);
			config.VoicesNames[channel.Id.ToString()] = name;

			await m_databaseProvider.SetConfig(config);
			await Context.Respond(new EmbedBuilder().WithDescription("Done"), true);
		}

		[SlashCommand("add_creator", "Add creator of voice room")]
		[RequireBotPermission(BotPermissions.UtilsAddVoice)]
		[RequireUserPermission(UserPermissions.UtilsAddVoice)]
		public async Task AddCreator(
			[Summary("", "")]
			IVoiceChannel channel,
			[MinLength(1), MaxLength(50),
			 Summary("template", "created channel name, default - Party #{Count}")]
			string name)
		{
			if (await channel.GetCategoryAsync() is not { } category)
				throw new Exception("Voice channel must have a category");

			var config = await m_databaseProvider.GetConfig(Context.Guild.Id);
			config.Channels[channel.Id.ToString()] = category.Id;
			config.VoicesNames[channel.Id.ToString()] = name;

			await m_databaseProvider.SetConfig(config);
			await RespondAsync("Done",
				allowedMentions: AllowedMentions.None,
				ephemeral: true);
		}
		
		[SlashCommand("remove_creator", "Remove creator of voice room")]
		[RequireUserPermission(UserPermissions.UtilsAddVoice)]
		public async Task RemoveCreator(
			[Summary("", "")]
			IVoiceChannel channel)
		{
			if (await channel.GetCategoryAsync() is null)
			{
				await RespondAsync("Voice channel must have a category",
					allowedMentions: AllowedMentions.None,
					ephemeral: true);
				return;
			}

			var config = await m_databaseProvider.GetConfig(Context.Guild.Id);
			config.Channels.Remove(channel.Id.ToString());
			config.VoicesNames.Remove(channel.Id.ToString());

			await m_databaseProvider.SetConfig(config);
			await RespondAsync("Done",
				allowedMentions: AllowedMentions.None,
				ephemeral: true);
		}
		
		[SlashCommand("get_creators", "Get creators of voice rooms")]
		[RequireUserPermission(UserPermissions.UtilsAddVoice)]
		public async Task GetCreators()
		{
			var config = await m_databaseProvider.GetConfig(Context.Guild.Id);
			if (config.Channels.Count < 1)
			{
				await RespondAsync("None",
					allowedMentions: AllowedMentions.None,
					ephemeral: true);
				return;
			}

			var txt = new StringBuilder();
			foreach (var (k, _) in config.Channels)
				txt.Append("<#").Append(k).Append('>').Append('\n');

			await RespondAsync(txt.ToString(),
				allowedMentions: AllowedMentions.None,
				ephemeral: true);
		}
	}
}