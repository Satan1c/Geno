using Database;
using Database.Models;
using Discord;
using Discord.WebSocket;
using Geno.Utils.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Geno.Handlers;

public class GuildEvents
{
	private readonly DatabaseProvider m_databaseProvider;

	public GuildEvents(IServiceProvider services)
	{
		var client = services.GetRequiredService<DiscordShardedClient>();
		m_databaseProvider = services.GetRequiredService<DatabaseProvider>();

		client.UserVoiceStateUpdated += OnUserVoiceStateUpdated;
	}

	public async Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
	{
		if (user is not SocketGuildUser guildUser
		    || guildUser.IsBot
		    || !await m_databaseProvider.HasDocument(guildUser.Guild.Id))
			return;

		var config = await m_databaseProvider.GetConfig(guildUser.Guild.Id, true);
		var guildUserId = guildUser.Id.ToString();
		var afterChannelId = after.VoiceChannel?.Id.ToString() ?? "";

		if (before.VoiceChannel is { } beforeChannel
		    && config.Voices.TryGetValue(guildUserId, out var voiceId)
		    && beforeChannel.Id == voiceId)
		{
			await OnDeleteChannel(beforeChannel, guildUserId, guildUser, config);
		}

		if (after.VoiceChannel is { } afterVoiceChannel && config.Channels.TryGetValue(afterChannelId, out var categoryId))
		{
			await OnCreateChannel(afterChannelId, afterVoiceChannel, categoryId, config, guildUser);
		}

		await m_databaseProvider.SetConfig(config);
	}

	private static async Task OnDeleteChannel(SocketVoiceChannel before, string guildUserId, SocketGuildUser guildUser, GuildDocument config)
	{
		var firstUser = before.ConnectedUsers.FirstOrDefault(x => !x.IsBot && x.Id != guildUser.Id);
		if (firstUser is null)
		{
			await guildUser.Guild
				.GetChannel(before.Id)
				.DeleteAsync();
		}
		else
		{
			await before.ModifyAsync(properties =>
			{
				properties.Name = config.VoicesNames[before.Id.ToString()].FormatWith(firstUser);
				properties.PermissionOverwrites = before.PermissionOverwrites
					.Where(x => x.TargetType == PermissionTarget.User && x.TargetId != guildUser.Id)
					.Append(new Overwrite(firstUser.Id, PermissionTarget.User, new OverwritePermissions(manageChannel: PermValue.Allow)))
					.ToArray();
			});
			config.Voices.Add(firstUser.Id.ToString(), before.Id);
		}

		config.Voices.Remove(guildUserId);
	}


	private static async Task OnCreateChannel(string afterChannelId, IGuildChannel afterVoiceChannel, ulong categoryId, GuildDocument config, SocketGuildUser guildUser)
	{
		var formatSource = new
		{
			Count = config.Voices.Count + 1,
			guildUser.DisplayName,
			guildUser.Username,
			UserTag = guildUser.UserTag(),
			ActivityName = guildUser.Activities.FirstOrDefault()?.Name ?? "Discord"
		};
		
		var voice = await guildUser.Guild.CreateVoiceChannelAsync(
			config.VoicesNames[afterChannelId].FormatWith(formatSource),
			properties =>
			{
				properties.CategoryId = categoryId;
				properties.Position = afterVoiceChannel?.Position ?? 0;
				properties.PermissionOverwrites = new Overwrite[]
				{
					new(guildUser.Id, PermissionTarget.User, new OverwritePermissions(manageChannel: PermValue.Allow)),
				};
			});

		config.Voices[guildUser.Id.ToString()] = voice.Id;
		
		
		await guildUser.ModifyAsync(x => x.Channel = voice);
	}
}