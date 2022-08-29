using Discord;
using Discord.Interactions;

namespace Geno.Errors;

public interface IErrorResolver
{
    public string ModuleName { get; }
    public string Resolve(IResult result, ICommandInfo command, IInteractionContext context);
}