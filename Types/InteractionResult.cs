using Discord.Interactions;

namespace Geno.Types;

public class InteractionResult : RuntimeResult
{
    public InteractionResult(InteractionCommandError? error, string reason) : base(error, reason)
    {
    }
}