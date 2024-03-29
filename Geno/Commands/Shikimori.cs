﻿namespace Geno.Commands;
/*
[Group("shikimori", "shikimori commands")]
public class Shikimori : InteractionModuleBase<ShardedInteractionContext>
{
	[Group("search", "search anime or manga")]
	public class SearchCommands : InteractionModuleBase<ShardedInteractionContext>
	{
		private readonly ShikimoriClient m_shikimoriClient;

		public SearchCommands(ShikimoriClient shikimoriClient)
		{
			m_shikimoriClient = shikimoriClient;
		}

		[SlashCommand("anime", "search anime by name")]
		public async Task SearchAnime(
			[Autocomplete(typeof(ShikimoriAnimeAutocompleteHandler))] [Summary("anime_name", "Anime name")]
			string query)
		{
			await Context.Interaction.DeferAsync();

			await Context.SearchResult(await FetchAnime(query));
		}

		[SlashCommand("manga", "search manga by name")]
		public async Task SearchManga(
			[Autocomplete(typeof(ShikimoriMangaAutocompleteHandler))] [Summary("manga_name", "Manga name")]
			string query)
		{
			await Context.Interaction.DeferAsync();

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
}*/