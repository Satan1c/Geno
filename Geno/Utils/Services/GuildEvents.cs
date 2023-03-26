using Database;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Geno.Utils.Services;

public class GuildEvents
{
	private readonly DatabaseProvider m_databaseProvider;

	public GuildEvents(IServiceProvider services)
	{
		var client = services.GetRequiredService<DiscordShardedClient>();
		m_databaseProvider = services.GetRequiredService<DatabaseProvider>();

		//m_client.MessageReceived += MessageReceived;
		client.UserVoiceStateUpdated += OnDeleteChannel;
		client.UserVoiceStateUpdated += OnCreateChannel;
	}

	/*public Task MessageReceived(SocketMessage message)
	{
		_ = Task.Run(async () =>
		{
			if (message.Source != MessageSource.User)
				return;
			var userMessage = (message as SocketUserMessage)!;

			var isLink = message.HasLink();
			var isInvite = await m_client.HasInvite(userMessage, true, true);

			if (!isLink && !isInvite)
				return;

			await (userMessage.Author as SocketGuildUser)!.SetTimeOutAsync(TimeSpan.FromSeconds(15));
		});
		
		return Task.CompletedTask;
	}*/

	public async Task OnDeleteChannel(SocketUser user, SocketVoiceState before, SocketVoiceState after)
	{
		if (user is not SocketGuildUser guildUser || !await m_databaseProvider.HasDocument(guildUser.Guild.Id))
			return;

		var config = await m_databaseProvider.GetConfig(guildUser.Guild.Id);
		var userId = guildUser.Id.ToString();

		if (config.Voices.TryGetValue(userId, out var value)
		    && before.VoiceChannel is { } beforeChannel
		    && beforeChannel.Id == value)
		{
			await guildUser.Guild
				.GetChannel(value)
				.DeleteAsync();

			config.Voices.Remove(userId);
			await m_databaseProvider.SetConfig(config);
		}
	}

	public async Task OnCreateChannel(SocketUser user, SocketVoiceState before, SocketVoiceState after)
	{
		if (user is not SocketGuildUser guildUser || !await m_databaseProvider.HasDocument(guildUser.Guild.Id))
			return;

		var config = await m_databaseProvider.GetConfig(guildUser.Guild.Id);

		if (after.VoiceChannel is { } afterChannel &&
		    config.Channels.ContainsKey(afterChannel.Id.ToString()))
		{
			//TODO: add permissions override to rename channel
			var count = config.Voices.Count + 1;
			var voice = await guildUser.Guild.CreateVoiceChannelAsync(
				"Party #" + count,
				properties => properties.CategoryId = config.Channels[after.VoiceChannel.Id.ToString()]);

			config.Voices[guildUser.Id.ToString()] = voice.Id;
			await m_databaseProvider.SetConfig(config);
			await guildUser.ModifyAsync(x => x.Channel = voice);
		}
	}
}