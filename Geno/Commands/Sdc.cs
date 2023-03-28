using Discord;
using Discord.Interactions;
using Geno.Handlers;
using Geno.Responsers.Success.Modules;
using Geno.Utils.Types;
using Microsoft.Extensions.DependencyInjection;
using SDC_Sharp.DiscordNet.Services;
using SDC_Sharp.DiscordNet.Types;

namespace Geno.Commands;

[Group("sdc", "sdc monitoring commands group")]
public class Sdc : InteractionModuleBase<ShardedInteractionContext>
{
	[Group("monitoring", "servers monitoring commands group")]
	public class MonitoringCommands : InteractionModuleBase<ShardedInteractionContext>
	{
		private readonly MonitoringService m_monitoring;

		/*public MonitoringCommands(MonitoringService monitoring)
		{
			ClientEvents.OnLog(new LogMessage(LogSeverity.Verbose, $"{nameof(Sdc)}.{nameof(MonitoringCommands)}", "Initializing"));
			
			m_monitoring = monitoring;
			
			ClientEvents.OnLog(new LogMessage(LogSeverity.Verbose, $"{nameof(Sdc)}.{nameof(MonitoringCommands)}", "Initialized"));
		}*/
		public MonitoringCommands(IServiceProvider serviceProvider)
		{
			ClientEvents.OnLog(new LogMessage(LogSeverity.Verbose, $"{nameof(Sdc)}.{nameof(MonitoringCommands)}", "Initializing"));
			
			m_monitoring = serviceProvider.GetRequiredService<MonitoringService>();
			
			ClientEvents.OnLog(new LogMessage(LogSeverity.Verbose, $"{nameof(Sdc)}.{nameof(MonitoringCommands)}", "Initialized"));
		}

		[SlashCommand("guild_info", "show guild info from site")]
		public async Task<RuntimeResult> GetGuild(ulong guildId)
		{
			//var id = ulong.Parse(guildId);
			var guild = await m_monitoring.GetGuild(guildId, true);
			await Context.GuildInfo(guild);

			return new Result(true, null, false, false);
		}

		//[SlashCommand("guild_rates", "show guild info from site")]
		public async Task GetGuildRates(ulong guildId)
		{
			//var id = ulong.Parse(guildId);
			var id = guildId;
			var rates = m_monitoring.GetGuildRates(id, true);
			var guild = m_monitoring.GetGuild(id, true);

			await Context.GuildRatesInfo(guild, rates);
		}
	}

	[Group("nika", "servers monitoring commands group")]
	public class NikaCommands : InteractionModuleBase<ShardedInteractionContext>
	{
		private readonly BlacklistService m_blacklistService;

		/*public NikaCommands(BlacklistService blacklistService)
		{
			ClientEvents.OnLog(new LogMessage(LogSeverity.Verbose, $"{nameof(Sdc)}.{nameof(NikaCommands)}", "Initializing"));
			
			m_blacklistService = blacklistService;
			
			ClientEvents.OnLog(new LogMessage(LogSeverity.Verbose, $"{nameof(Sdc)}.{nameof(NikaCommands)}", "Initialized"));
		}*/
		public NikaCommands(IServiceProvider serviceProvider)
		{
			ClientEvents.OnLog(new LogMessage(LogSeverity.Verbose, $"{nameof(Sdc)}.{nameof(NikaCommands)}", "Initializing"));
			
			m_blacklistService = serviceProvider.GetRequiredService<BlacklistService>();
			
			ClientEvents.OnLog(new LogMessage(LogSeverity.Verbose, $"{nameof(Sdc)}.{nameof(NikaCommands)}", "Initialized"));
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
				warns = new UserWarns
				{
					Id = user.Id,
					User = user,
					Type = "user",
					Warns = 0
				};
			}

			await Context.WarnsInfo(warns);
		}
	}
}