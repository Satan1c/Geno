using System.Text;
using Discord;
using Discord.Interactions;
using Geno.Types;
using Geno.Utils;
using MongoDB.Driver;

namespace Geno.Commands;

[Group("utils", "utils commands group")]
public class Utils : InteractionModuleBase<ShardedInteractionContext>
{

    [Group("add", "add commands sub group")]
    public class AddUtils : InteractionModuleBase<ShardedInteractionContext>
    {
        private readonly IMongoCollection<GuildDocument> m_guildConfigs;

        public AddUtils(IMongoClient mongo)
            => m_guildConfigs = mongo.GetDatabase("main").GetCollection<GuildDocument>("guilds");

        [SlashCommand("voice_rooms_channel", "sets base voice-rooms channel")]
        [RequireBotPermission(ChannelPermission.ManageChannels)]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        public async Task AddVoiceChannel([ChannelTypes(ChannelType.Voice)] IVoiceChannel channel)
        {
            if (await channel.GetCategoryAsync() is not ICategoryChannel category)
            {
                await RespondAsync("Voice channel must have a category",
                    allowedMentions: AllowedMentions.None,
                    ephemeral: true);
                return;
            }

            var config = await m_guildConfigs.GetConfig(Context.Guild.Id);
            config.Channels[channel.Id.ToString()] = category.Id;

            await m_guildConfigs.SetConfig(config);
            await RespondAsync("Done",
                allowedMentions: AllowedMentions.None,
                ephemeral: true);
        }
    }

    [Group("remove", "remove commands sub group")]
    public class RemoveUtils : InteractionModuleBase<ShardedInteractionContext>
    {
        private readonly IMongoCollection<GuildDocument> m_guildConfigs;

        public RemoveUtils(IMongoClient mongo)
            => m_guildConfigs = mongo.GetDatabase("main").GetCollection<GuildDocument>("guilds");

        [SlashCommand("voice_rooms_channel", "removes base voice-rooms channel")]
        [RequireBotPermission(ChannelPermission.ManageChannels)]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        public async Task RemoveVoiceChannel([ChannelTypes(ChannelType.Voice)] IVoiceChannel channel)
        {
            if (await channel.GetCategoryAsync() is not ICategoryChannel category)
            {
                await RespondAsync("Voice channel must have a category",
                    allowedMentions: AllowedMentions.None,
                    ephemeral: true);
                return;
            }

            var config = await m_guildConfigs.GetConfig(Context.Guild.Id);
            config.Channels.Remove(channel.Id.ToString());

            await m_guildConfigs.SetConfig(config);
            await RespondAsync("Done",
                allowedMentions: AllowedMentions.None,
                ephemeral: true);
        }
    }

    [Group("get", "get commands sub group")]
    public class GetUtils : InteractionModuleBase<ShardedInteractionContext>
    {
        private readonly IMongoCollection<GuildDocument> m_guildConfigs;

        public GetUtils(IMongoClient mongo)
            => m_guildConfigs = mongo.GetDatabase("main").GetCollection<GuildDocument>("guilds");

        [SlashCommand("voice_rooms_channel", "gets base voice-rooms channel")]
        [RequireBotPermission(ChannelPermission.ManageChannels)]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        public async Task GetVoiceChannel()
        {
            var config = await m_guildConfigs.GetConfig(Context.Guild.Id);
            if (config.Channels.Count < 1)
            {
                await RespondAsync("None",
                    allowedMentions: AllowedMentions.None,
                    ephemeral: true);
                return;
            }

            var txt = new StringBuilder();
            foreach (var (k, v) in config.Channels) txt.Append("<#").Append(k).Append('>').Append('\n');

            await RespondAsync(txt.ToString(),
                allowedMentions: AllowedMentions.None,
                ephemeral: true);
        }
    }
}