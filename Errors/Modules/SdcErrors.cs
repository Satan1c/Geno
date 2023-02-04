using Discord;
using Discord.Interactions;
using Geno.Commands;
using Geno.Utils.Extensions;
using Geno.Utils.Types;

namespace Geno.Errors.Modules;

public class SdcErrors : IErrorResolver
{
	private const string m_module = nameof(Sdc);
	public string ModuleName => m_module;

	public EmbedBuilder Resolve(IResult result, ICommandInfo command, IInteractionContext context, EmbedBuilder embed)
	{
		return context.GetLocale() switch
		{
			UserLocales.Russian => Russian(command.MethodName, result, embed),
			_ => English(command.MethodName, result, embed)
		};
	}

	private EmbedBuilder English(string commandMethodName, IResult result, EmbedBuilder embed)
	{
		return commandMethodName switch
		{
			nameof(Sdc.MonitoringCommands.GetGuild) => result.Error switch
			{
				InteractionCommandError.Exception
					=> new Func<IResult, EmbedBuilder>(x =>
							embed.WithDescription(x is ExecuteResult { Exception: FormatException _ }
								? "Invalid guild id"
								: $"sdc default \n{commandMethodName} {result.Error} {result.ErrorReason}")
						)
						.Invoke(result),
				null => embed.WithDescription("null"),
				_ => throw new ArgumentOutOfRangeException()
			},
			_ => embed.WithDescription($"_ \n{commandMethodName} {result.Error} {result.ErrorReason}")
		};
	}

	private EmbedBuilder Russian(string commandMethodName, IResult result, EmbedBuilder embed)
	{
		return commandMethodName switch
		{
			nameof(Sdc.MonitoringCommands.GetGuild) => result.Error switch
			{
				InteractionCommandError.Exception
					=> new Func<IResult, EmbedBuilder>(x =>
						embed.WithDescription(x is ExecuteResult { Exception: FormatException _ }
							? "Не верно указан айди"
							: $"sdc стандартная \n{commandMethodName} {result.Error} {result.ErrorReason}")
					).Invoke(result),
				null => embed.WithDescription("null r"),
				_ => embed.WithDescription(result.ErrorReason)
			},
			_ => embed.WithDescription($"_ \n{commandMethodName} {result.Error} {result.ErrorReason}")
		};
	}
}