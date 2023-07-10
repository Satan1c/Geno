using Discord;
using Discord.Interactions;
using Geno.Responsers.Success.Modules;
using SDC_Sharp.DiscordNet.Models;
using SDC_Sharp.DiscordNet.Services;

namespace Geno.Commands;

[Group("sdc", "sdc monitoring commands group")]
public class Sdc : InteractionModuleBase<ShardedInteractionContext>
{
	[Group("monitoring", "servers monitoring commands group")]
	public class MonitoringCommands : InteractionModuleBase<ShardedInteractionContext>
	{
		private readonly MonitoringService m_monitoring;

		public MonitoringCommands(MonitoringService monitoring)
		{
			m_monitoring = monitoring;
		}

		[SlashCommand("guild_info", "show guild info from site")]
		public async Task GetGuild(ulong guildId)
		{
			var guild = await m_monitoring.GetGuild(guildId, true);
			await Context.GuildInfo(guild);
		}

		//[SlashCommand("guild_rates", "show guild info from site")]
		public async Task GetGuildRates(ulong guildId)
		{
			var rates = m_monitoring.GetGuildRates(guildId, true);
			var guild = m_monitoring.GetGuild(guildId, true);

			await Context.GuildRatesInfo(guild, rates);
		}
	}

	[Group("nika", "servers monitoring commands group")]
	public class NikaCommands : InteractionModuleBase<ShardedInteractionContext>
	{
		private readonly BlacklistService m_blacklistService;

		public NikaCommands(BlacklistService blacklistService)
		{
			m_blacklistService = blacklistService;
		}

		[SlashCommand("warns", "show guild info from site")]
		public async Task GetWarns(IUser user)
		{
			UserWarns warns;
			var id = user.Id;

			try
			{
				warns = await m_blacklistService.GetWarns(id, true);
				await Context.WarnsInfo(warns);
				return;
			}
			catch
			{
				warns = new UserWarns(user.Id, 0, "user", user);
			}

			await Context.WarnsInfo(warns);
		}
	}
}