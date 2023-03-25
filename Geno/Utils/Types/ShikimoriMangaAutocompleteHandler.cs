using Discord;
using Discord.Interactions;
using Geno.Utils.Extensions;
using Geno.Utils.Services;
using Microsoft.Extensions.DependencyInjection;
using ShikimoriSharp;
using ShikimoriSharp.Settings;

namespace Geno.Utils.Types;


	public class ShikimoriMangaAutocompleteHandler : AutocompleteHandler
	{
		private ShikimoriClient? m_shikimoriClient = null;

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
				
				var resultsRaw = await m_shikimoriClient.Mangas.GetBySearch(new MangaRequestSettings
				{
					search = userInput,
					limit = 10
				});
				var results = resultsRaw.Select(x => 
					new AutocompleteResult(
						context.GetLocale() == UserLocales.Russian 
							? (string.IsNullOrEmpty(x.Russian) ? x.Name : x.Russian)
							: x.Name,
					
						x.Name));return AutocompletionResult.FromSuccess(results.Take(10));
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
