using System.Diagnostics;
using System.Text;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Geno.Utils.Extensions;
using Geno.Utils.Types;

namespace Geno.Commands;

[Group("other", "other command group")]
public class Other : ModuleBase
{
	private static string GetUptime(ref TimeSpan time)
	{
		var days = TimeString(time.Days, 'd');
		var hours = TimeString(time.Hours, 'h');
		var minutes = TimeString(time.Minutes, 'm');
		var seconds = TimeString(time.Seconds, 's', "`0`s");

		return new StringBuilder().Append(days).Append(hours).Append(minutes).Append(seconds).ToString();
	}

	private static string TimeString(int value, char name, string? zero = null)
	{
		return value > 0
			? new StringBuilder().Append('`')
				.Append(value)
				.Append('`')
				.Append(name)
				.Append(' ').ToString()
			: zero ?? "";
	}

	[Group("bot", "commands group about bot")]
	public class BotCommands : ModuleBase
	{
		private readonly DiscordShardedClient m_client;

		public BotCommands(DiscordShardedClient client)
		{
			m_client = client;
		}

		[SlashCommand("status", "show bot stats")]
		public async Task PingCommand(bool clear = false)
		{
			if (clear && Context.User.Id == (await Context.Client.GetApplicationInfoAsync()).Owner.Id)
				GC.Collect();

			var process = Process.GetCurrentProcess();
			var ram = ((short)(process.WorkingSet64 / 1024 / 1024)).ToString();
			var uptime = DateTime.UtcNow - process.StartTime;
			var uptimeString = GetUptime(ref uptime);
			var embed = new EmbedBuilder().WithTitle("Bot stats");

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

			await Respond(embed);
		}
	}

	[Group("fetch", "fetch commands sub group")]
	public class FetchCommands : ModuleBase
	{
		[SlashCommand("guild", "fetch guild information by invite")]
		public async Task FetchGuild(string inviteCode)
		{
			RestGuild guild;
			var embed = new EmbedBuilder();

			if (Context.Client.TryGetInvite(inviteCode.Split("/")[^1], out var invite))
			{
				if (Context.Client.Rest.TryGetGuild(invite.GuildId ?? 0, out guild))
					embed = embed.ApplyData(invite).ApplyData(Context.Client.GetGuild(guild.Id));
				else
					embed.ApplyData(invite, true);
			}
			else if (Context.Client.Rest.TryGetGuild(ulong.Parse(inviteCode), out guild))
			{
				embed = embed.ApplyData(Context.Client.GetGuild(guild.Id));
			}

			if (embed.Length < 1)
				embed.WithDescription("Guild not found");

			await Respond(embed);
			await RespondAsync(embed: embed.Build(),
				allowedMentions: AllowedMentions.None);
		}

		[SlashCommand("user", "fetch user information by id")]
		public async Task FetchUser(IUser? rawUser = null)
		{
			if (!Context.Client.Rest.TryGetUser(rawUser?.Id ?? Context.User.Id, out var user))
			{
				await Respond(new EmbedBuilder().WithColor(Color.Red)
					.WithDescription("Can't get info about this user"));
				return;
			}

			var embed = new EmbedBuilder().ApplyData(user);

			if (Context.Guild is { } guild
			    && Context.Client.Rest.TryGetGuildUser(guild.Id, user.Id, out var guildUser))
				embed = embed.ApplyData(guildUser);

			await Respond(embed);
		}
	}
}