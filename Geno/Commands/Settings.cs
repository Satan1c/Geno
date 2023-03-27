using System.Text;
using Database;
using Discord;
using Discord.Interactions;
using Geno.Responsers.Success;
using Geno.Utils.StaticData;

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
	
	[Group("set", "set commands sub group")]
	public class SetUtils : InteractionModuleBase<ShardedInteractionContext>
	{
		[SlashCommand("voice_rooms_names", "Voice room name template; {Count},{DisplayName},{Username},{UserTag},{ActivityName}")]
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

			var config = await m_databaseProvider.GetConfig(Context.Guild.Id, true);
			config.VoicesNames[channel.Id.ToString()] = name;

			await m_databaseProvider.SetConfig(config);
			await Context.Respond(new EmbedBuilder().WithDescription("Done"), true);
		}
	}
	
	[Group("add", "add commands sub group")]
	public class AddUtils : InteractionModuleBase<ShardedInteractionContext>
	{
		[SlashCommand("voice_rooms_channel", "sets base voice-rooms channel")]
		[RequireBotPermission(BotPermissions.UtilsAddVoice)]
		[RequireUserPermission(UserPermissions.UtilsAddVoice)]
		public async Task AddVoiceChannel(
			IVoiceChannel channel,
			[MinLength(1), MaxLength(50), Summary("template", "created channel name, default - Party #{Count}")]
			string name = "Party #{Count}")
		{
			if (await channel.GetCategoryAsync() is not { } category)
				throw new Exception("Voice channel must have a category");

			var config = await m_databaseProvider.GetConfig(Context.Guild.Id, true);
			config.Channels[channel.Id.ToString()] = category.Id;
			config.VoicesNames[channel.Id.ToString()] = name;

			await m_databaseProvider.SetConfig(config);
			await RespondAsync("Done",
				allowedMentions: AllowedMentions.None,
				ephemeral: true);
		}
	}

	[Group("remove", "remove commands sub group")]
	public class RemoveUtils : InteractionModuleBase<ShardedInteractionContext>
	{
		[SlashCommand("voice_rooms_channel", "removes base voice-rooms channel")]
		[RequireBotPermission(ChannelPermission.ManageChannels)]
		[RequireUserPermission(ChannelPermission.ManageChannels)]
		public async Task RemoveVoiceChannel(
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

			var config = await m_databaseProvider.GetConfig(Context.Guild.Id, true);
			config.Channels.Remove(channel.Id.ToString());
            config.VoicesNames.Remove(channel.Id.ToString());

            await m_databaseProvider.SetConfig(config);
			await RespondAsync("Done",
				allowedMentions: AllowedMentions.None,
				ephemeral: true);
		}
	}

	[Group("get", "get commands sub group")]
	public class GetUtils : InteractionModuleBase<ShardedInteractionContext>
	{
		[SlashCommand("voice_rooms_channel", "gets base voice-rooms channel")]
		[RequireBotPermission(ChannelPermission.ManageChannels)]
		[RequireUserPermission(ChannelPermission.ManageChannels)]
		public async Task GetVoiceChannel()
		{
			var config = await m_databaseProvider.GetConfig(Context.Guild.Id, true);
			if (config.Channels.Count < 1)
			{
				await RespondAsync("None",
					allowedMentions: AllowedMentions.None,
					ephemeral: true);
				return;
			}

			var txt = new StringBuilder();
			foreach (var (k, _) in config.Channels) txt.Append("<#").Append(k).Append('>').Append('\n');

			await RespondAsync(txt.ToString(),
				allowedMentions: AllowedMentions.None,
				ephemeral: true);
		}
	}
}