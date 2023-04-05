using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Discord;
using Discord.Interactions;
using Geno.Utils.Extensions;
using WaifuPicsApi.Enums;

namespace Geno.Handlers;

public class SfwAutocompleteHandler : AutocompleteHandler
{
	private static AutocompleteResult[] s_sfwCategories = Array.Empty<AutocompleteResult>();

	public SfwAutocompleteHandler()
	{
		if (s_sfwCategories.Any()) return;

		s_sfwCategories = new AutocompleteResult[31];
		UnsafeExtensions.GenerateCategoriesUnsafe(ref s_sfwCategories);
	}

	public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
		IInteractionContext context,
		IAutocompleteInteraction autocompleteInteraction,
		IParameterInfo parameter,
		IServiceProvider services)
	{
		try
		{
			var userInput = autocompleteInteraction.Data.Current.Value.ToString()!;
			return AutocompletionResult.FromSuccess(s_sfwCategories.FilterResultUnsafe(ref userInput));
		}
		catch (Exception e)
		{
			await ClientEvents.OnLog(
				new LogMessage(
					LogSeverity.Error,
					nameof(SfwAutocompleteHandler) + " " + nameof(GenerateSuggestionsAsync),
					e.Message,
					e));
			return AutocompletionResult.FromError(e);
		}
	}
}

file static class UnsafeExtensions
{
	public static AutocompleteResult[] FilterResultUnsafe(this AutocompleteResult[] categories, ref string userInput)
	{
		var checker = (AutocompleteResult result, string input) =>
			(result.Name.StartsWith(input, StringComparison.InvariantCultureIgnoreCase), result);

		return categories.GetAutocompletesUnsafe(ref userInput, ref checker);

		/*var result = new AutocompleteResult[5];
		ref var startResult = ref MemoryMarshal.GetArrayDataReference(result);
		ref var endResult = ref Unsafe.Add(ref startResult, result.Length);
		
		ref var start = ref MemoryMarshal.GetArrayDataReference(categories);
		ref var end = ref Unsafe.Add(ref start, categories.Length);

		while (Unsafe.IsAddressLessThan(ref start, ref end) && Unsafe.IsAddressLessThan(ref startResult, ref endResult))
		{
			if (start.Name.StartsWith(userInput, StringComparison.InvariantCultureIgnoreCase))
			{
				startResult = start;
				startResult = ref Unsafe.Add(ref startResult, 1);
			}
			
			start = ref Unsafe.Add(ref start, 1);
		}
		
		return result;*/
	}

	public static void GenerateCategoriesUnsafe(ref AutocompleteResult[] sfwCategories)
	{
		ref var start = ref MemoryMarshal.GetArrayDataReference(sfwCategories);
		ref var end = ref Unsafe.Add(ref start, sfwCategories.Length);

		var counter = 0;
		while (Unsafe.IsAddressLessThan(ref start, ref end))
		{
			if (Enum.TryParse<SfwCategory>(counter.ToString(), out var category))
			{
				var name = category.EnumToString();
				start = new AutocompleteResult(name, name);
			}

			counter++;
			start = ref Unsafe.Add(ref start, 1);
		}

		var results = new AutocompleteResult[counter];
		sfwCategories.CopyTo(results, 0);
		sfwCategories = results;
	}
}