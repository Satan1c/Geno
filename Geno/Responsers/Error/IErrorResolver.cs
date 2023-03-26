using Discord;
using Discord.Interactions;

namespace Geno.Responsers.Error;

public interface IErrorResolver
{
	public string ModuleName { get; }
	public EmbedBuilder Resolve(IResult result, ICommandInfo command, IInteractionContext context, EmbedBuilder embed);
}