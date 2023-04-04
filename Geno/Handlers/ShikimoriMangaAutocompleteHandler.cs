using Discord;
using Discord.Interactions;
using Geno.Utils.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ShikimoriService;
using ShikimoriSharp.Bases;

namespace Geno.Handlers;

public class ShikimoriMangaAutocompleteHandler : AutocompleteHandler
{
	private ShikimoriClient? m_shikimoriClient;

	public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
		IInteractionContext context,
		IAutocompleteInteraction autocompleteInteraction,
		IParameterInfo parameter,
		IServiceProvider services)
	{
		m_shikimoriClient ??= services.GetRequiredService<ShikimoriClient>();

		try
		{
			var userInput = autocompleteInteraction.Data.Current.Value.ToString()!;
			var search = await m_shikimoriClient.GetManga(userInput, 5);
			if (search == null || search.Length < 1)
				return AutocompletionResult.FromSuccess(Array.Empty<AutocompleteResult>());

			var locale = context.GetLocale();
			var tasks = search.Select(async x => (AnimeMangaIdBase)await m_shikimoriClient.GetManga(x.Id)).ToArray();
			Task.WaitAll(tasks, CancellationToken.None);
			
			return AutocompletionResult.FromSuccess(tasks.FilterResultUnsafe(ref locale));
		}
		catch (Exception e)
		{
			await ClientEvents.OnLog(
				new LogMessage(
					LogSeverity.Error,
					nameof(ShikimoriMangaAutocompleteHandler) + " " + nameof(GenerateSuggestionsAsync),
					e.Message,
					e));
			return AutocompletionResult.FromError(e);
		}
	}
}