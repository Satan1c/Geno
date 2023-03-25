using System.Collections;

namespace Geno.Utils;

public static class Utils
{
	public static IDictionary<string, string> GetEnv()
	{
		return ((Hashtable)Environment.GetEnvironmentVariables()).Cast<DictionaryEntry>()
			.ToDictionary(
				kvp
					=> (string)kvp.Key, kvp => (string)kvp.Value!);
	}
}

/*public class TypeReader<T> : TypeReader where T : struct, Enum
{
	public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
	{
		return Task.FromResult(Enum.TryParse<T>(input, out var result)
			? TypeReaderResult.FromSuccess(result)
			: TypeReaderResult.FromError(CommandError.ParseFailed, "CommandError.ParseFailed"));
	}
}*/