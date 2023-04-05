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
		    || !await m_databaseProvider.HasGuild(guildUser.Guild.Id))
			return;

		var config = await m_databaseProvider.GetConfig(guildUser.Guild.Id).ConfigureAwait(false);
		var guildUserId = guildUser.Id.ToString();
		var afterChannelId = after.VoiceChannel?.Id.ToString() ?? "";

		if (before.VoiceChannel is { } beforeChannel
		    && config.Voices.TryGetValue(guildUserId, out var voiceId)
		    && beforeChannel.Id == voiceId)
			await OnDeleteChannel(beforeChannel, guildUserId, guildUser, config);

		if (after.VoiceChannel is { } afterVoiceChannel &&
		    config.Channels.TryGetValue(afterChannelId, out var categoryId))
			await OnCreateChannel(afterChannelId, afterVoiceChannel, categoryId, config, guildUser);

		await m_databaseProvider.SetConfig(config);
	}

	private static async Task OnDeleteChannel(SocketVoiceChannel before,
		string guildUserId,
		SocketGuildUser guildUser,
		GuildDocument config)
	{
		if (before.ConnectedUsers.TryGetValue(guildUser.Id, out var firstUser))
		{
			var name = config.VoicesNames[before.Id.ToString()].FormatWith(firstUser);
			var perms = before.PermissionOverwrites.GetPermissions(guildUser.Id, firstUser.Id);

			await before.ModifyAsync(properties =>
			{
				properties.Name = name;
				properties.PermissionOverwrites = perms;
			});
			config.Voices.Add(firstUser.Id.ToString(), before.Id);
		}
		else
		{
			await guildUser.Guild
				.GetChannel(before.Id)
				.DeleteAsync();
		}

		config.Voices.Remove(guildUserId);
	}

	private static async Task OnCreateChannel(string afterChannelId,
		IGuildChannel afterVoiceChannel,
		ulong categoryId,
		GuildDocument config,
		SocketGuildUser guildUser)
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
				properties.Position = afterVoiceChannel.Position;
				properties.PermissionOverwrites = new Overwrite[]
				{
					new(guildUser.Id, PermissionTarget.User, new OverwritePermissions(manageChannel: PermValue.Allow))
				};
			});

		config.Voices[guildUser.Id.ToString()] = voice.Id;

		await guildUser.ModifyAsync(x => x.Channel = voice);
	}
}