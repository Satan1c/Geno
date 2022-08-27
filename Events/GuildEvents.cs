using Discord;
using Discord.WebSocket;
using Geno.Database;
using Geno.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Geno.Events;

public class GuildEvents
{
    private readonly DiscordShardedClient m_client;
    private readonly DatabaseProvider m_databaseProvider;

    public GuildEvents(IServiceProvider services)
    {
        m_client = services.GetRequiredService<DiscordShardedClient>();
        m_databaseProvider = services.GetRequiredService<DatabaseProvider>();

        m_client.MessageReceived += MessageReceived;
        m_client.UserVoiceStateUpdated += OnDeleteChannel;
        m_client.UserVoiceStateUpdated += OnCreateChannel;
    }

    public async Task MessageReceived(SocketMessage message)
    {
        if (message.Source != MessageSource.User)
            return;
        var userMessage = (message as SocketUserMessage)!;

        var isLink = message.HasLink();
        var isInvite = await m_client.HasInvite(userMessage, true, true).ConfigureAwait(false);

        if (!isLink && !isInvite)
            return;

        await (userMessage.Author as SocketGuildUser)!.SetTimeOutAsync(TimeSpan.FromSeconds(15));
    }

    public async Task OnDeleteChannel(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        if (user is not SocketGuildUser guildUser || !await m_databaseProvider.HasDocument(guildUser.Guild.Id))
            return;

        var config = await m_databaseProvider.GetConfig(guildUser.Guild.Id);
        var userId = guildUser.Id.ToString();

        if (config.Voices.ContainsKey(userId)
            && before.VoiceChannel is SocketVoiceChannel beforeChannel
            && beforeChannel.Id == config.Voices[userId])
        {
            await guildUser.Guild
                .GetChannel(config.Voices[userId])
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

        if (after.VoiceChannel is SocketVoiceChannel afterChannel &&
            config.Channels.ContainsKey(afterChannel.Id.ToString()))
        {
            var voice = await guildUser.Guild
                .CreateVoiceChannelAsync(
                    $"Party #{config.Voices.Count + 1}",
                    properties => properties.CategoryId = config.Channels[after.VoiceChannel.Id.ToString()]);

            config.Voices[guildUser.Id.ToString()] = voice.Id;
            await m_databaseProvider.SetConfig(config);
            await guildUser.ModifyAsync(x => x.Channel = voice);
        }
    }
}