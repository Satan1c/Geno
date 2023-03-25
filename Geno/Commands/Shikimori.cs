using Discord.Interactions;
using Geno.Responses.Modules;
using Geno.Utils.Types;
using ShikimoriSharp;
using ShikimoriSharp.Classes;
using ShikimoriSharp.Settings;

namespace Geno.Commands;

[Group("shikimori", "shikimori commands")]
public partial class Shikimori : InteractionModuleBase<ShardedInteractionContext>
{
	[Group("search", "search anime or manga")]
	public class SearchCommands : InteractionModuleBase<ShardedInteractionContext>
	{
		private readonly ShikimoriClient m_shikimoriClient;

		public SearchCommands(ShikimoriClient shikimoriClient)
		{
			m_shikimoriClient = shikimoriClient;
		}
		
		[SlashCommand("anime", "search anime")]
		public async Task SearchAnime([Autocomplete(typeof(ShikimoriAnimeAutocompleteHandler))]string query)
		{
			await Context.Interaction.DeferAsync();
			query = query.ToLower();
			
			await Context.SearchResult(await FetchAnime(query));
		}
		
		[SlashCommand("manga", "search manga")]
		public async Task SearchManga([Autocomplete(typeof(ShikimoriMangaAutocompleteHandler))]string query)
		{
			await Context.Interaction.DeferAsync();
			query = query.ToLower();

			await Context.SearchResult(await FetchManga(query));
		}
		
		private async Task<AnimeID?> FetchAnime(string query)
		{
			var animeSettings = new AnimeRequestSettings
			{
				search = query,
				limit = 1
			};
			var animeRaw = (await m_shikimoriClient.Animes.GetAnime(animeSettings)).FirstOrDefault();
			return animeRaw == null ? null : await m_shikimoriClient.Animes.GetAnime(animeRaw.Id);
		}
		
		private async Task<MangaID?> FetchManga(string query)
		{
			var mangaSettings = new MangaRequestSettings
			{
				search = query,
				limit = 1
			};
			var mangaRaw = (await m_shikimoriClient.Mangas.GetBySearch(mangaSettings)).FirstOrDefault();
			return mangaRaw == null ? null : await m_shikimoriClient.Mangas.GetById(mangaRaw.Id);
		}
	}

}