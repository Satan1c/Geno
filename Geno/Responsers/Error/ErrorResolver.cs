using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
		var types = assembly.DefinedTypes.ToArray();

		ref var start = ref MemoryMarshal.GetArrayDataReference(types);
		ref var end = ref Unsafe.Add(ref start, types.Length);

		while (Unsafe.IsAddressLessThan(ref start, ref end))
		{
			if (s_validType.IsAssignableFrom(start) && start.IsClass)
			{
				var args = Array.Empty<object>();
				var resolver = (start.DeclaredConstructors.First(x => x.IsPublic).Invoke(args) as IErrorResolver)!;
				resolver.LocalizationManager = localizationManager;
				s_list[resolver.ModuleName] = resolver;
			}

			start = ref Unsafe.Add(ref start, 1);
		}
	}

	public static EmbedBuilder Resolve(IResult result, ICommandInfo command, IInteractionContext ctx)
	{
		//ClientEvents.OnLog(new LogMessage(LogSeverity.Error, command.MethodName, result.ErrorReason));
		var name = command?.Module?.GetTopLevelModule()?.Name ?? "unknown";
		var embed = new EmbedBuilder().WithColor(Color.Red);

		return !s_list.ContainsKey(name)
			? embed.WithDescription(result.ErrorReason ?? "unknown")
			: s_list[name].Resolve(result, command, ctx, embed);
	}
}