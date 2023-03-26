using System.Reflection;
using Discord;
using Discord.Interactions;
using Geno.Utils.Extensions;
using Localization;

namespace Geno.Responsers.Error;

public static class ErrorResolver
{
	private static readonly TypeInfo s_validType = typeof(IErrorResolver).GetTypeInfo();
	private static readonly Dictionary<string, IErrorResolver> s_list = new();

	public static void Init(Assembly assembly, LocalizationManager localizationManager)
	{
		foreach (var definedType in assembly.DefinedTypes.ToArray())
		{
			if (!s_validType.IsAssignableFrom(definedType) || !definedType.IsClass) continue;

			var resolver = (definedType.DeclaredConstructors.First().Invoke(Array.Empty<object>()) as IErrorResolver)!;
			resolver.localizationManager = localizationManager;
			s_list[resolver.ModuleName] = resolver;
		}
	}

	public static EmbedBuilder Resolve(IResult result, ICommandInfo command, IInteractionContext ctx)
	{
		//ClientEvents.OnLog(new LogMessage(LogSeverity.Error, command.MethodName, result.ErrorReason));
		var name = command.Module.GetTopLevelModule().Name!;
		var embed = new EmbedBuilder().WithColor(Color.Red);

		return !s_list.ContainsKey(name)
			? embed.WithDescription(result.ErrorReason ?? "unknown")
			: s_list[name].Resolve(result, command, ctx, embed);
	}
}