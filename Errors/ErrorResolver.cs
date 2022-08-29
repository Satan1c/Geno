using System.Reflection;
using Discord;
using Discord.Interactions;
using Geno.Utils;

namespace Geno.Errors;

public class ErrorResolver
{
    private static readonly TypeInfo m_validType = typeof(IErrorResolver).GetTypeInfo();
    private static readonly Dictionary<string, IErrorResolver> m_list = new ();

    public static void Init(Assembly assembly)
    {
        foreach (var definedType in assembly.DefinedTypes)
        {
            if (!m_validType.IsAssignableFrom(definedType) || !definedType.IsClass) continue;

            var resolver = (definedType.DeclaredConstructors.First().Invoke(Array.Empty<object>()) as IErrorResolver)!;
            m_list[resolver.ModuleName] = resolver;
        }
    }
    
    public static string Resolve(IResult result, ICommandInfo command, IInteractionContext ctx) 
        => m_list[command.Module.GetTopLevelModule().Name].Resolve(result, command, ctx);

    public static UserLocales GetLocale(string userLocale)
    {
        Enum.TryParse<UserLocales>(userLocale, out var lang);
        return lang;
    }
}