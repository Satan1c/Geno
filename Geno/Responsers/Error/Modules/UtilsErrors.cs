using Discord;
using Discord.Interactions;
using Geno.Utils.Extensions;

namespace Geno.Responsers.Error.Modules;

public class UtilsErrors : IErrorResolver
{
	private const string m_module = nameof(Geno.Commands.Utils);
	public string ModuleName => m_module;

	public EmbedBuilder Resolve(IResult result, ICommandInfo command, IInteractionContext context, EmbedBuilder embed)
	{
		return context.GetLocale() switch
		{
			_ => English(command.MethodName, result, command, context, embed)
		};
	}

	private EmbedBuilder English(string commandMethodName, IResult result, ICommandInfo command,
		IInteractionContext context, EmbedBuilder embed)
	{
		return commandMethodName switch
		{
			nameof(Geno.Commands.Utils.AddUtils.AddVoiceChannel) => result.Error switch
			{
				InteractionCommandError.Exception => embed.WithDescription(result.ErrorReason),
				InteractionCommandError.UnmetPrecondition => embed.WithDescription(result.ErrorReason),
				_ => embed.WithDescription("default")
			},
			_ => embed.WithDescription("default aa")
		};
	}
}