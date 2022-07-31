using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Geno.Utils;

public static class Utils
{
    
}

public class TypeReader<T> : TypeReader where T : struct, Enum
{
    public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input,
        IServiceProvider services)
    {
        return Task.FromResult(Enum.TryParse<T>(input, out var result)
            ? TypeReaderResult.FromSuccess(result)
            : TypeReaderResult.FromError(CommandError.ParseFailed, "CommandError.ParseFailed"));
    }
}