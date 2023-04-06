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