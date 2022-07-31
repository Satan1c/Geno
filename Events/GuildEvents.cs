using Discord;
using Discord.WebSocket;
using Geno.Types;
using Geno.Utils;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Geno.Events;

public class GuildEvents
{
    private readonly DiscordShardedClient m_client;
    private readonly IMongoCollection<GuildDocument> m_guildConfigs;
    public GuildEvents(IServiceProvider services)
    {
        m_client = services.GetRequiredService<DiscordShardedClient>();
        var db = services.GetRequiredService<IMongoClient>().GetDatabase("main");
        m_guildConfigs = db.GetCollection<GuildDocument>("guilds");
        
        m_client.MessageReceived += MessageReceived;
        m_client.UserVoiceStateUpdated += OnUserVoiceStateUpdated;
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
    
    public async Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        if (user is not SocketGuildUser guildUser 
            || await m_guildConfigs.CountDocumentsAsync(x => x.Id == guildUser.Guild.Id) < 1) 
            return;

        var config = await m_guildConfigs.GetConfig(guildUser.Guild.Id);
        var userId = guildUser.Id.ToString();

        if (config.Voices.ContainsKey(userId) 
            && before.VoiceChannel is SocketVoiceChannel beforeChannel 
            && beforeChannel.Id == config.Voices[userId])
        {
            await guildUser.Guild
                .GetChannel(config.Voices[userId])
                .DeleteAsync();

            config.Voices.Remove(userId);
            await m_guildConfigs.SetConfig(config);
        }
        
        if (after.VoiceChannel is SocketVoiceChannel afterChannel 
            && config.Channels.ContainsKey(afterChannel.Id.ToString()))
        {
            var voice = await guildUser.Guild
                .CreateVoiceChannelAsync(
                    $"Party #{config.Voices.Count + 1}",
                    properties => properties.CategoryId = config.Channels[after.VoiceChannel.Id.ToString()]);

            config.Voices[userId] = voice.Id;
            await m_guildConfigs.SetConfig(config);
            await guildUser.ModifyAsync(x => x.Channel = voice);
        }
    }
}