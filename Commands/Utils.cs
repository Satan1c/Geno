using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Geno.Types;
using Geno.Utils;
using MongoDB.Driver;

namespace Geno.Commands;

[Group("utils", "utils commands group")]
public class Utils : InteractionModuleBase<ShardedInteractionContext>
{
    private readonly IMongoCollection<GuildDocument> m_guildConfigs;

    public Utils(DiscordShardedClient client, IMongoClient mongo)
    {
        var db = mongo.GetDatabase("main");
        m_guildConfigs = db.GetCollection<GuildDocument>("guilds");

        client.UserVoiceStateUpdated += OnUserVoiceStateUpdated;
    }

    private async Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        if (user is not SocketGuildUser guildUser) return;

        var config = await m_guildConfigs.GetConfig(guildUser.Guild.Id);
        var userId = guildUser.Id.ToString();
        
        if (config.Voices.ContainsKey(userId))
        {
            if ((after.VoiceChannel != null && after.VoiceChannel.Id != config.Voices[userId]) 
                || (before.VoiceChannel != null && before.VoiceChannel.Id == config.VoiceId)) return;

            await guildUser.Guild
                .GetChannel(config.Voices[userId])
                .DeleteAsync();

            config.Voices.Remove(userId);
            await m_guildConfigs.SetConfig(config);
            return;
        }

        var voice = await guildUser.Guild
            .CreateVoiceChannelAsync(
                $"Party #{config.Voices.Count + 1}",
                properties => properties.CategoryId = config.CategoryId);

        config.Voices[userId] = voice.Id;
        await m_guildConfigs.SetConfig(config);
        await guildUser.ModifyAsync(x => x.Channel = voice);
    }

    [Group("set", "set commands sub group")]
    public class SetUtils : InteractionModuleBase<ShardedInteractionContext>
    {
        private readonly IMongoCollection<GuildDocument> m_guildConfigs;

        public SetUtils(IMongoClient mongo)
            => m_guildConfigs = mongo.GetDatabase("main").GetCollection<GuildDocument>("guilds");

        [SlashCommand("voice_rooms_channel", "sets base voice-rooms channel")]
        [RequireBotPermission(ChannelPermission.ManageChannels)]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        public async Task SetVoiceChannel([ChannelTypes(ChannelType.Voice)] IVoiceChannel channel)
        {
            if (await channel.GetCategoryAsync() is not ICategoryChannel category)
            {
                await RespondAsync("Voice channel must have a category",
                    allowedMentions: AllowedMentions.None,
                    ephemeral: true);
                return;
            }

            var config = await m_guildConfigs.GetConfig(Context.Guild.Id);
            config.CategoryId = category.Id;
            config.VoiceId = channel.Id;

            await m_guildConfigs.SetConfig(config);
            await RespondAsync("Done",
                allowedMentions: AllowedMentions.None,
                ephemeral: true);
        }
    }
}