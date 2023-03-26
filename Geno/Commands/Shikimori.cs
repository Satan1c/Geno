using Discord.Interactions;
using Geno.Responsers.Success.Modules;
using Geno.Utils.Types;
using ShikimoriSharp.Classes;

namespace Geno.Commands;

[Group("shikimori", "shikimori commands")]
public partial class Shikimori : InteractionModuleBase<ShardedInteractionContext>
{
	[Group("search", "search anime or manga")]
	public class SearchCommands : InteractionModuleBase<ShardedInteractionContext>
	{
		private readonly ShikimoriService.ShikimoriClient m_shikimoriClient;

		public SearchCommands(ShikimoriService.ShikimoriClient shikimoriClient)
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
			var animeRaw = (await m_shikimoriClient.GetAnime(query));
			return animeRaw == null ? null : await m_shikimoriClient.GetAnime(animeRaw.Id);
		}
		
		private async Task<MangaID?> FetchManga(string query)
		{
			var mangaRaw = (await m_shikimoriClient.GetManga(query));
			return mangaRaw == null ? null : await m_shikimoriClient.GetManga(mangaRaw.Id);
		}
	}

}