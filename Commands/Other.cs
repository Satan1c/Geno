using System.Diagnostics;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Geno.Utils;

namespace Geno.Commands;

[Group("other", "other command group")]
public class Other : InteractionModuleBase<ShardedInteractionContext>
{
    [Group("bot", "commands group about bot")]
    public class BotCommands : InteractionModuleBase<ShardedInteractionContext>
    {
        private double cpuUsage;
        public BotCommands()
        {
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var startTime = DateTime.UtcNow;
                    var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;

                    await Task.Delay(500);

                    var endTime = DateTime.UtcNow;
                    var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;

                    var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                    var totalMsPassed = (endTime - startTime).TotalMilliseconds;

                    var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

                    cpuUsage = cpuUsageTotal * 100;
                }
            });
        }
        
        [SlashCommand("stats", "show bot stats")]
        public async Task PingCommand()
        {
            var embed = new EmbedBuilder()
                .WithTitle("Bot stats:");
            
            var process = Process.GetCurrentProcess();

            {
                var ram = (short)(process.WorkingSet64 / 1024 / 1024);
                var uptime = process.TotalProcessorTime;
                var uptimeString = string.Format(
                    (uptime.Days > 0 ? "`{0:D1}`d " : "") +
                    (uptime.Hours > 0 ? "`{1:D1}`h " : "") +
                    (uptime.Minutes > 0 ? "`{2:D1}`m " : "") +
                    (uptime.Seconds > 0 ? "`{3:D1}`s" : "`0`s"),

                    uptime.Days.ToString(),
                    uptime.Hours.ToString(),
                    uptime.Minutes.ToString(),
                    uptime.Seconds.ToString()
                );
                
                embed.AddField("RAM usage:", $"`{ram.ToString()}`mb", true)
                    .AddField("UP time:", uptimeString, true)
                    //.AddField(EmbedExtensions.Empty, EmbedExtensions.Empty, true);
                    .AddField("CPU usage", $"`{cpuUsage.ToString()}`%", true);
            }

            if (Context.Guild is SocketGuild guild)
            {
                var currentShard = Context.Client.GetShardFor(guild);

                embed.AddField("Current server shard:",
                    $"`{currentShard.ShardId.ToString()}`: `{currentShard.Latency.ToString()}`ms");
            }

            foreach (var shard in Context.Client.Shards)
                embed.AddField($"`{shard.ShardId.ToString()}`:", $"`{shard.Latency.ToString()}`ms", true);

            await RespondAsync(embed: embed.Build(),
                allowedMentions: AllowedMentions.None);
        }
    }

    [Group("fetch", "fetch commands sub group")]
    public class FetchCommands : InteractionModuleBase<ShardedInteractionContext>
    {
        [SlashCommand("guild", "fetch guild information by invite")]
        public async Task FetchGuild(string inviteCode)
        {
            if (!Context.Client.TryGetInvite(inviteCode.Split("/")[^1], out var invite))
            {
                await RespondAsync("Can't get info about this guild",
                    allowedMentions: AllowedMentions.None,
                    ephemeral: true);

                return;
            }

            var embed = new EmbedBuilder().ApplyData(invite);

            if (Context.Client.Rest.TryGetGuild(invite.GuildId ?? 0, out var guild))
                embed = embed.ApplyData(guild);

            await RespondAsync(embed: embed.Build(),
                allowedMentions: AllowedMentions.None);
        }

        [SlashCommand("user", "fetch user information by id")]
        public async Task FetchUser(IUser rawUser)
        {
            if (!Context.Client.Rest.TryGetUser(rawUser.Id, out var user))
                throw new Exception("Can't get info about this user");
            

            var embed = new EmbedBuilder().ApplyData(user);

            if (Context.Guild is SocketGuild guild && Context.Client.Rest.TryGetGuildUser(guild.Id, user.Id, out var guildUser))
                embed = embed.ApplyData(guildUser);

            await RespondAsync(embed: embed.Build(),
                allowedMentions: AllowedMentions.None);
        }
    }
}