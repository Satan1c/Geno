using Discord;
using Discord.Interactions;
using Geno.Commands;
using Geno.Utils.Extensions;
using Geno.Utils.Types;
using Localization;

namespace Geno.Responsers.Error.Modules;

public class SdcErrors : IErrorResolver
{
	private const string m_module = nameof(Sdc);
	public string ModuleName => m_module;
	public LocalizationManager localizationManager { get; set; }

	public EmbedBuilder Resolve(IResult result, ICommandInfo command, IInteractionContext context, EmbedBuilder embed)
	{
		var locale = localizationManager.GetCategory("sdc").GetDataFor("sdc").GetForLocale(context);
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