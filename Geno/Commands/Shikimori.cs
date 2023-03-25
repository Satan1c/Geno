using Discord;
using Discord.Interactions;
using Geno.Responses;
using Geno.Responses.Modules;
using Geno.Utils;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ShikimoriSharp;
using ShikimoriSharp.AdditionalRequests;
using ShikimoriSharp.Classes;
using ShikimoriSharp.Settings;

namespace Geno.Commands;

[Group("shikimori", "shikimori commands")]
public class Shikimori : InteractionModuleBase<ShardedInteractionContext>
{
	private readonly ShikimoriClient m_shikimoriClient;

	public Shikimori(ShikimoriClient shikimoriClient)
	{
		m_shikimoriClient = shikimoriClient;
	}
	
	[SlashCommand("search", "search anime or manga")]
	public async Task Search(string query, Mode searchMode)
	{
		await Context.Interaction.DeferAsync();
		query = query.ToLower();

		AnimeID? anime =  null;
		MangaID? manga =  null;
		
		if (searchMode.HasMode(Mode.Anime))
		{
			anime = await SearchAnime(query);
		}
		else if (searchMode.HasMode(Mode.Manga))
		{
			manga = await SearchManga(query);
		}
		
		await Context.SearchResult(anime, manga);
	}
	
	private async Task<AnimeID?> SearchAnime(string query)
	{
		var animeSettings = new AnimeRequestSettings
		{
			search = query,
			limit = 1
		};
		var animeRaw = (await m_shikimoriClient.Animes.GetAnime(animeSettings)).FirstOrDefault();
		return animeRaw == null ? null : await m_shikimoriClient.Animes.GetAnime(animeRaw.Id);
	}
	
	private async Task<MangaID?> SearchManga(string query)
	{
		var mangaSettings = new MangaRequestSettings
		{
			search = query,
			limit = 1
		};
		var mangaRaw = (await m_shikimoriClient.Mangas.GetBySearch(mangaSettings)).FirstOrDefault();
		return mangaRaw == null ? null : await m_shikimoriClient.Mangas.GetById(mangaRaw.Id);
	}

	private async Task<Related[]?> GetRelated(MangaID manga)
	{
		return await m_shikimoriClient.Mangas.GetRelated(manga.Id);
	}
	
	private async Task<Related[]?> GetRelated(AnimeID anime)
	{
		return await m_shikimoriClient.Animes.GetRelated(anime.Id);
	}

	[JsonConverter(typeof(StringEnumConverter))]
	[Flags]
	public enum Mode : byte
	{
		Anime = 1 << 0,
		Manga = 1 << 1
	}
}