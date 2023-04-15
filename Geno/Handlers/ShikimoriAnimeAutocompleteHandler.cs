using Discord;
using Discord.Interactions;
using Geno.Utils.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ShikimoriService;
using ShikimoriSharp.Bases;

namespace Geno.Handlers;

public class ShikimoriAnimeAutocompleteHandler : AutocompleteHandler
{
	private static ShikimoriClient? s_shikimoriClient;

	public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
		IInteractionContext context,
		IAutocompleteInteraction autocompleteInteraction,
		IParameterInfo parameter,
		IServiceProvider services)
	{
		s_shikimoriClient ??= services.GetRequiredService<ShikimoriClient>();

		try
		{
			var userInput = autocompleteInteraction.Data.Current.Value.ToString()!.Trim();
			if (string.IsNullOrEmpty(userInput))
				return AutocompletionResult.FromSuccess(Array.Empty<AutocompleteResult>());

			var search = await s_shikimoriClient.GetAnime(userInput, 5);
			if (search == null || search.Length < 1)
				return AutocompletionResult.FromSuccess(Array.Empty<AutocompleteResult>());

			var locale = context.GetLocale();
			var tasks = await Task.WhenAll(
				search.Select(async x =>
					(AnimeMangaIdBase)(await s_shikimoriClient.GetAnime(x.Id))!));

			var results = tasks.FilterResultUnsafe(ref locale);
			return AutocompletionResult.FromSuccess(results);
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