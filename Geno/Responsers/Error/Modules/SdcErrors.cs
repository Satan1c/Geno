using Discord;
using Discord.Interactions;
using Geno.Commands;
using Geno.Utils.Extensions;
using Localization;
using Localization.Models;

namespace Geno.Responsers.Error.Modules;

public class SdcErrors : IErrorResolver
{
	private const string m_module = nameof(Sdc);
	public string ModuleName => m_module;
	private static Data s_data;

	public LocalizationManager LocalizationManager
	{
		set => s_data = value.GetCategory("error").GetDataFor("sdc");
	}

	public EmbedBuilder Resolve(IResult result, ICommandInfo command, IInteractionContext context, EmbedBuilder embed)
	{
		var locale = s_data.GetForLocale(context);
		var defaultLocale = locale["default"].FormatWith(new { command.MethodName, result.Error, result.ErrorReason });

		return new EmbedBuilder().WithTitle("Sdc error").WithDescription(
			command.MethodName switch
			{
				nameof(Sdc.MonitoringCommands.GetGuild) =>
					result.Error switch
					{
						InteractionCommandError.Exception => locale["exception"],
						null => "null",
						_ => defaultLocale
					},
				_ => defaultLocale
			});
	}
}