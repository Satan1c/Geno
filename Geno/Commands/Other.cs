using System.Diagnostics;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Geno.Handlers;
using Geno.Utils.Extensions;
using Geno.Utils.Types;

namespace Geno.Commands;

[Group("other", "other command group")]
public class Other : InteractionModuleBase<ShardedInteractionContext>
{
	[Group("bot", "commands group about bot")]
	public class BotCommands : InteractionModuleBase<ShardedInteractionContext>
	{
		private readonly DiscordShardedClient m_client;

		public BotCommands(DiscordShardedClient client)
		{
			ClientEvents.OnLog(new LogMessage(LogSeverity.Verbose, $"{nameof(Other)}.{nameof(BotCommands)}", "Initializing"));
			
			m_client = client;
			
			ClientEvents.OnLog(new LogMessage(LogSeverity.Verbose, $"{nameof(Other)}.{nameof(BotCommands)}", "Initialized"));
		}

		[SlashCommand("status", "show bot stats")]
		public async Task PingCommand(bool clear = false)
		{
			if (clear && Context.User.Id == (await Context.Client.GetApplicationInfoAsync()).Owner.Id)
				GC.Collect();

			var embed = new EmbedBuilder().WithTitle("Bot stats");
			var process = Process.GetCurrentProcess();
			var ram = ((short)(process.WorkingSet64 / 1024 / 1024)).ToString();
			var uptime = DateTime.UtcNow - process.StartTime;
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

			embed.AddField("Servers: ", $"`{m_client.Shards.Select(x => x.Guilds.Count).Sum().ToString()}`", true)
				.AddField("RAM usage:", $"`{ram}`mb", true)
				.AddField("UP time:", uptimeString, true);

			if (Context.Guild is { } guild)
			{
				var currentShard = Context.Client.GetShardFor(guild);

				embed.AddField("Current server shard:",
					$"`{currentShard.ShardId.ToString()}`: `{currentShard.Latency.ToString()}`ms");
			}

			foreach (var shard in Context.Client.Shards.ToArray())
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
			RestGuild guild;
			var embed = new EmbedBuilder();

			if (Context.Client.TryGetInvite(inviteCode.Split("/")[^1], out var invite))
			{
				embed = embed.ApplyData(invite);
				if (Context.Client.Rest.TryGetGuild(invite.GuildId ?? 0, out guild))
					embed = embed.ApplyData(Context.Client.GetGuild(guild.Id));
			}
			else if (Context.Client.Rest.TryGetGuild(ulong.Parse(inviteCode), out guild))
			{
				embed = embed.ApplyData(Context.Client.GetGuild(guild.Id));
			}

			if (embed.Length < 1)
				embed.WithDescription("Guild not found");

			await RespondAsync(embed: embed.Build(),
				allowedMentions: AllowedMentions.None);
		}

		[SlashCommand("user", "fetch user information by id")]
		public Task<RuntimeResult> FetchUser(IUser rawUser)
		{
			if (!Context.Client.Rest.TryGetUser(rawUser.Id, out var user))
				return Result.GetTaskFor(
					false,
					null,
					true,
					false,
					InteractionCommandError.Unsuccessful,
					"Can't get info about this user");

			var embed = new EmbedBuilder().ApplyData(user);

			if (Context.Guild is { } guild && Context.Client.Rest.TryGetGuildUser(guild.Id, user.Id, out var guildUser))
				embed = embed.ApplyData(guildUser);

			return Result.GetTaskFor(true, embed, false, false);
		}
	}
}