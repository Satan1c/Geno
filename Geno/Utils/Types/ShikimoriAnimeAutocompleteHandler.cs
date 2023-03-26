﻿using Discord;
using Discord.Interactions;
using Geno.Utils.Extensions;
using Geno.Utils.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Geno.Utils.Types;

public class ShikimoriAnimeAutocompleteHandler : AutocompleteHandler
{
	private ShikimoriService.ShikimoriClient? m_shikimoriClient = null;

	public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
		IInteractionContext context,
		IAutocompleteInteraction autocompleteInteraction,
		IParameterInfo parameter,
		IServiceProvider services)
	{
		m_shikimoriClient ??= services.GetRequiredService<ShikimoriService.ShikimoriClient>();
		
		try
		{
			var userInput = autocompleteInteraction.Data.Current.Value.ToString()!;
			var search = await m_shikimoriClient.GetAnime(userInput, 5);
			if (search == null || search.Length < 1) return AutocompletionResult.FromSuccess(Array.Empty<AutocompleteResult>());
			
			var tasks = search.Select(async x => await m_shikimoriClient.GetAnime(x.Id)).ToArray();
			Task.WaitAll(tasks, CancellationToken.None);
			
			var resultsRaw = tasks.Select(x => x.Result).Where(x => x != null).ToArray();
			var results = resultsRaw.Select(x => x!.AutocompleteResultFrom(context.GetLocale()));
			return AutocompletionResult.FromSuccess(results.Take(5));
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