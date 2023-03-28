using Discord;
using Discord.Interactions;
using Geno.Handlers;
using Geno.Responsers.Success.Modules;
using Geno.Utils.Types;
using Microsoft.Extensions.DependencyInjection;
using ShikimoriService;
using ShikimoriSharp.Classes;

namespace Geno.Commands;

[Group("shikimori", "shikimori commands")]
public class Shikimori : InteractionModuleBase<ShardedInteractionContext>
{
	[Group("search", "search anime or manga")]
	public class SearchCommands : InteractionModuleBase<ShardedInteractionContext>
	{
		private readonly ShikimoriClient m_shikimoriClient;

		/*public SearchCommands(ShikimoriClient shikimoriClient)
		{
			ClientEvents.OnLog(new LogMessage(LogSeverity.Verbose, $"{nameof(Shikimori)}.{nameof(SearchCommands)}", "Initializing"));
			
			m_shikimoriClient = shikimoriClient;
			
			ClientEvents.OnLog(new LogMessage(LogSeverity.Verbose, $"{nameof(Shikimori)}.{nameof(SearchCommands)}", "Initialized"));
		}*/
		public SearchCommands(IServiceProvider serviceProvider)
		{
			ClientEvents.OnLog(new LogMessage(LogSeverity.Verbose, $"{nameof(Shikimori)}.{nameof(SearchCommands)}", "Initializing"));
			
			m_shikimoriClient = serviceProvider.GetRequiredService<ShikimoriClient>();
			
			ClientEvents.OnLog(new LogMessage(LogSeverity.Verbose, $"{nameof(Shikimori)}.{nameof(SearchCommands)}", "Initialized"));
		}

		[SlashCommand("anime", "search anime by name")]
		public async Task SearchAnime(
			[Autocomplete(typeof(ShikimoriAnimeAutocompleteHandler))]
			[Summary("anime_name", "Anime name")]
			string query)
		{
			await Context.Interaction.DeferAsync();
			query = query.ToLower();

			await Context.SearchResult(await FetchAnime(query));
		}

		[SlashCommand("manga", "search manga by name")]
		public async Task SearchManga(
			[Autocomplete(typeof(ShikimoriMangaAutocompleteHandler))]
			[Summary("manga_name", "Manga name")]
			string query)
		{
			await Context.Interaction.DeferAsync();
			query = query.ToLower();

			await Context.SearchResult(await FetchManga(query));
		}

		private async Task<AnimeID?> FetchAnime(string query)
		{
			var animeRaw = await m_shikimoriClient.GetAnime(query);
			return animeRaw == null ? null : await m_shikimoriClient.GetAnime(animeRaw.Id);
		}

		private async Task<MangaID?> FetchManga(string query)
		{
			var mangaRaw = await m_shikimoriClient.GetManga(query);
			return mangaRaw == null ? null : await m_shikimoriClient.GetManga(mangaRaw.Id);
		}
	}
}