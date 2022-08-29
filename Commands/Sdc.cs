using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using SDC_Sharp.DiscordNet.Services;
using SDC_Sharp.Types;

namespace Geno.Commands;

[Group("sdc", "sdc monitoring commands group")]
public class Sdc : InteractionModuleBase<ShardedInteractionContext>
{
    [Group("monitoring", "servers monitoring commands group")]
    public class MonitoringCommands : InteractionModuleBase<ShardedInteractionContext>
    {
        private readonly MonitoringService m_monitoring;

        public MonitoringCommands(IServiceProvider provider)
        {
            m_monitoring = provider.GetRequiredService<MonitoringService>();
        }

        [SlashCommand("guild", "show guild info from site")]
        public async Task GetGuild(ulong id)
        {
            var guild = await m_monitoring.GetGuild(id);
            var embed = new EmbedBuilder()
                .WithAuthor(guild.Name, guild.Avatar, guild.Url)
                .WithDescription($"`{(string.Join("`, `", guild.Tags.Select(x => x.TagsToString())))}`")
                .AddField("Members", $"`{guild.Members.ToString()}`")
                .AddField("Online", $"`{guild.Online.ToString()}`")
                .AddField("Badge", $"`{guild.Badges.BadgeToString()}`")
                .AddField("Boost", $"`{guild.Boost.BoostLevelToString()}`");

            await RespondAsync(embed: embed.Build(),
                allowedMentions: AllowedMentions.None);
        }
    }
}