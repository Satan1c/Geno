using System.Text;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using EnkaAPI;
using Geno.Database;
using Geno.Utils;

namespace Geno.Commands;

[Group("utils", "utils commands group")]
[EnabledInDm(false)]
public class Utils : InteractionModuleBase<ShardedInteractionContext>
{
    [Group("set", "set commands group")]
    public class SetUtils : InteractionModuleBase<ShardedInteractionContext>
    {
        private readonly EnkaApiClient m_enkaApiClient;
        private readonly DatabaseProvider m_databaseProvider;

        public SetUtils(DatabaseProvider databaseProvider, EnkaApiClient enkaApiClient)
        {
            m_databaseProvider = databaseProvider;
            m_enkaApiClient = enkaApiClient;
        }
        
        [MessageCommand("Rank info")]
        public async Task RankInfo(IMessage message)
        {
            if (!uint.TryParse(message.Content, out var uid))
                throw new Exception("Invalid AR value");
            if (message.Author.Id != Context.User.Id)
                throw new Exception("That isn't your message");

            var data = (await m_enkaApiClient.GetData(uid)).PlayerInfo;
            var doc = await UpdateDoc(message, Context.User.Id.ToString());

            await UpdateRoles(data.AdventureRank, doc, Context.Guild.GetUser(message.Author.Id));
            await RespondAsync("Done", ephemeral: true);
        }
        
        [MessageCommand("Set ranks roles")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task SetRankRoles(IMessage message)
        {
            //await Context.Interaction.DeferAsync();
            var pairs = message.Content.Split('\n');
            var cfg = await m_databaseProvider.GetConfig(Context.Guild.Id);
            cfg.RankRoles = new Dictionary<string, ulong[]>();

            string[] pair;
            for (byte i = 0; i < pairs.Length; i++)
            {
                pair = pairs[i].Split('-');
                var k = byte.Parse(pair[0]);
                var v = pair[1].Split(',').Select(x => ulong.Parse(x)).ToArray();
                cfg.RankRoles[k.ToString()] = v;
            }

            await m_databaseProvider.SetConfig(cfg);
            await RespondAsync("Done", ephemeral: true);
        }
        
        [SlashCommand("rank", "set rank for user")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task Setup(IUser user, byte newRank)
        {
            var member = Context.Guild.GetUser(user.Id)!;
            var cfg = await m_databaseProvider.GetConfig(Context.Guild.Id);
            var role = cfg.RankRoles.GetPerfectRole(newRank.ToString());
            var remove = member.Roles
                .Where(x => cfg.RankRoles.Values.Any(y => y.Contains(x.Id)))
                .Where(x => !role.Contains(x.Id))
                .Select(x => x.Id)
                .ToArray();

            if (remove.Any())
                await member.RemoveRolesAsync(remove);

            await member.AddRolesAsync(role);

            await RespondAsync("Done", ephemeral: true);
        }
        
        private static async Task UpdateRoles(uint adventureRank, GuildDocument doc, SocketGuildUser member)
        {
            var role = doc.RankRoles.GetPerfectRole(adventureRank.ToString());
            var remove = member.Roles
                .Where(x => doc.RankRoles.Values.Any(y => y.Contains(x.Id)))
                .Where(x => !role.Contains(x.Id))
                .Select(x => x.Id)
                .ToArray();

            if (remove.Any())
                await member.RemoveRolesAsync(remove);

            await member.AddRolesAsync(role);
        }
    
        private async Task<GuildDocument> UpdateDoc(IMessage message, string userId)
        {
            var doc = await m_databaseProvider.GetConfig(Context.Guild.Id);
            if (doc.UserScreens.ContainsKey(userId) && doc.UserScreens[userId] != message.Id)
                await message.Channel.DeleteMessageAsync(doc.UserScreens[userId]);

            doc.UserScreens[userId] = message.Id;
            await m_databaseProvider.SetConfig(doc);

            return await Task.FromResult(doc);
        }
    }
    
    [Group("add", "add commands sub group")]
    public class AddUtils : InteractionModuleBase<ShardedInteractionContext>
    {
        private readonly DatabaseProvider m_databaseProvider;

        public AddUtils(DatabaseProvider databaseProvider)
        {
            m_databaseProvider = databaseProvider;
        }

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

            var config = await m_databaseProvider.GetConfig(Context.Guild.Id);
            config.Channels[channel.Id.ToString()] = category.Id;

            await m_databaseProvider.SetConfig(config);
            await RespondAsync("Done",
                allowedMentions: AllowedMentions.None,
                ephemeral: true);
        }
    }

    [Group("remove", "remove commands sub group")]
    public class RemoveUtils : InteractionModuleBase<ShardedInteractionContext>
    {
        private readonly DatabaseProvider m_databaseProvider;

        public RemoveUtils(DatabaseProvider databaseProvider)
        {
            m_databaseProvider = databaseProvider;
        }

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

            var config = await m_databaseProvider.GetConfig(Context.Guild.Id);
            config.Channels.Remove(channel.Id.ToString());

            await m_databaseProvider.SetConfig(config);
            await RespondAsync("Done",
                allowedMentions: AllowedMentions.None,
                ephemeral: true);
        }
    }

    [Group("get", "get commands sub group")]
    public class GetUtils : InteractionModuleBase<ShardedInteractionContext>
    {
        private readonly DatabaseProvider m_databaseProvider;

        public GetUtils(DatabaseProvider databaseProvider)
        {
            m_databaseProvider = databaseProvider;
        }

        [SlashCommand("voice_rooms_channel", "gets base voice-rooms channel")]
        [RequireBotPermission(ChannelPermission.ManageChannels)]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        public async Task GetVoiceChannel()
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
            foreach (var (k, v) in config.Channels) txt.Append("<#").Append(k).Append('>').Append('\n');

            await RespondAsync(txt.ToString(),
                allowedMentions: AllowedMentions.None,
                ephemeral: true);
        }
        
        [SlashCommand("ranks_config", "show current ranks config")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task GetConfig()
        {
            var config = await m_databaseProvider.GetConfig(Context.Guild.Id);
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
}