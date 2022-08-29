using Discord;
using Discord.Interactions;

namespace Geno.Errors;

public class SdcErrors : IErrorResolver
{
    public string ModuleName => MODULE;
    private const string MODULE = nameof(Commands.Sdc);
    
    public string Resolve(IResult result, ICommandInfo command, IInteractionContext context)
    {
        return ErrorResolver.GetLocale(context.Interaction.UserLocale) switch
        {
            UserLocales.English => English(command.MethodName, result),
            _ => English(command.MethodName, result)
        };
    }

    private string English(string commandMethodName, IResult result)
    {
        return result.Error switch
        {
            InteractionCommandError.UnknownCommand => "Unknown command",
            InteractionCommandError.ConvertFailed => "None ConvertFailed",
            InteractionCommandError.BadArgs => "Invalid number or arguments",
            InteractionCommandError.Exception => $"Command exception: {result.ErrorReason}",
            InteractionCommandError.Unsuccessful => "Command could not be executed",
            InteractionCommandError.UnmetPrecondition => $"Unmet Precondition: {result.ErrorReason}",
            InteractionCommandError.ParseFailed => "None ParseFailed",
            null => "mull",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}