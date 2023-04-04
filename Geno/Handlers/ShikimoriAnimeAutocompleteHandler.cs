using Discord;
using Discord.Interactions;
using Geno.Utils.Extensions;
using Geno.Utils.Types;
using Microsoft.Extensions.DependencyInjection;
using ShikimoriService;
using ShikimoriSharp.Classes;

namespace Geno.Handlers;

public class ShikimoriAnimeAutocompleteHandler : AutocompleteHandler
{
	private static ShikimoriClient? s_shikimoriClient = null;

	public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
		IInteractionContext context,
		IAutocompleteInteraction autocompleteInteraction,
		IParameterInfo parameter,
		IServiceProvider services)
	{
		s_shikimoriClient ??= services.GetRequiredService<ShikimoriClient>();
		
		try
		{
			var userInput = autocompleteInteraction.Data.Current.Value.ToString()!;
			var search = await s_shikimoriClient.GetAnime(userInput, 5);
			if (search == null || search.Length < 1)
				return AutocompletionResult.FromSuccess(Array.Empty<AutocompleteResult>());

			var locale = context.GetLocale();
			var tasks = search.Select(async x => await s_shikimoriClient.GetAnime(x.Id)).ToArray();
			Task.WaitAll(tasks, CancellationToken.None);

			return AutocompletionResult.FromSuccess(tasks.FilterResultUnsafe(ref locale));
		}
		catch (Exception e)
		{
			await ClientEvents.OnLog(
				new LogMessage(
					LogSeverity.Error,
					nameof(ShikimoriAnimeAutocompleteHandler) + " " + nameof(GenerateSuggestionsAsync),
					e.Message,
					e));
			return AutocompletionResult.FromError(e);
		}
	}
}

file static class UnsafeExtensions
{
	public static AutocompleteResult[] FilterResultUnsafe(this Task<AnimeID?>[] tasks, ref UserLocales locale)
	{
		var checker = (Task<AnimeID?> task, UserLocales locales) =>
		{
			var result = task.Result;
			return (result != null, result.AutocompleteResultFrom(locales));
		};
		
		return tasks.GetAutocompletesUnsafe(ref locale, ref checker);
		
		/*var results = new AutocompleteResult[5];
		ref var startResult = ref MemoryMarshal.GetArrayDataReference(results);
		ref var endResult = ref Unsafe.Add(ref startResult, results.Length);

		ref var start = ref MemoryMarshal.GetArrayDataReference(tasks);
		ref var end = ref Unsafe.Add(ref start, tasks.Length);

		while (Unsafe.IsAddressLessThan(ref start, ref end) && Unsafe.IsAddressLessThan(ref startResult, ref endResult))
		{
			var result = start.Result;
			if (result != null)
			{
				startResult = result.AutocompleteResultFrom(locale);
				startResult = ref Unsafe.Add(ref startResult, 1);
			}

			start = ref Unsafe.Add(ref start, 1);
		}

		return results;*/
	}
}