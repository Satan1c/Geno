using CacheManager.Core;
using ComposableAsync;
using Microsoft.Extensions.Logging;
using RateLimiter;
using ShikimoriSharp.Bases;
using ShikimoriSharp.Classes;
using ShikimoriSharp.Settings;

namespace ShikimoriService;

public class ShikimoriClient
{
	private static readonly ICacheManager<AnimeID> s_cacheAnime = CacheFactory.Build<AnimeID>(part =>
		part.WithMicrosoftMemoryCacheHandle().WithExpiration(ExpirationMode.Sliding, TimeSpan.FromMinutes(10)));

	private static readonly ICacheManager<Anime> s_cacheAnimeRaw = CacheFactory.Build<Anime>(part =>
		part.WithMicrosoftMemoryCacheHandle().WithExpiration(ExpirationMode.Sliding, TimeSpan.FromMinutes(10)));

	private static readonly ICacheManager<MangaID> s_cacheManga = CacheFactory.Build<MangaID>(part =>
		part.WithMicrosoftMemoryCacheHandle().WithExpiration(ExpirationMode.Sliding, TimeSpan.FromMinutes(10)));

	private static readonly ICacheManager<Manga> s_cacheMangaRaw = CacheFactory.Build<Manga>(part =>
		part.WithMicrosoftMemoryCacheHandle().WithExpiration(ExpirationMode.Sliding, TimeSpan.FromMinutes(10)));

	private readonly TimeLimiter m_firstLimit = TimeLimiter.GetFromMaxCountByInterval(5, TimeSpan.FromSeconds(1));

	private readonly ShikimoriSharp.ShikimoriClient m_shikimoriClient;

	public ShikimoriClient(ILogger logger)
	{
		m_shikimoriClient = new ShikimoriSharp.ShikimoriClient(logger, new ClientSettings(
			"Geno",
			"mkGRM2ud5xmOqUl5bvZkUbFV-zqjQimkQ-W5hhPBFR0",
			"OlOUNsD14GN2TM6WHwaUaEuqrkFS7LGKJfwtHvyf6Ck"
		));
	}

	public async ValueTask<MangaID?> GetManga(long id)
	{
		var mangaId = id.ToString();
		if (s_cacheManga.Exists(mangaId)) return s_cacheManga.Get(mangaId);

		await m_firstLimit;

		var manga = await m_shikimoriClient.Mangas.GetById(id).ConfigureAwait(false);
		if (manga != null)
			s_cacheManga.Put(manga.Id.ToString(), manga);

		return manga;
	}

	public async ValueTask<Manga?> GetManga(string name)
	{
		if (!string.IsNullOrEmpty(name.Trim()) && s_cacheMangaRaw.Exists(name))
			return s_cacheMangaRaw.Get(name);

		await m_firstLimit;

		var manga = (await GetManga(name, 1))?.FirstOrDefault();
		if (manga != null)
			s_cacheMangaRaw.Put(manga.Name, manga);

		return manga;
	}

	public async ValueTask<Manga[]?> GetManga(string name, byte limit)
	{
		if (!string.IsNullOrEmpty(name.Trim()) && s_cacheMangaRaw.Exists(name))
			return new[] { s_cacheMangaRaw.Get(name) };

		await m_firstLimit;

		var manga = await m_shikimoriClient.Mangas.GetBySearch(new MangaRequestSettings
		{
			search = name,
			limit = limit > 0 ? limit > 50 ? 50 : limit : 1
		});

		foreach (var m in manga)
			if (m != null)
				s_cacheMangaRaw.Put(m.Name, m);

		return manga;
	}

	public async ValueTask<AnimeID?> GetAnime(long id)
	{
		var animeId = id.ToString();
		if (s_cacheAnime.Exists(animeId)) return s_cacheAnime.Get(animeId);

		await m_firstLimit;

		var anime = await m_shikimoriClient.Animes.GetAnime(id);
		if (anime != null)
			s_cacheAnime.Put(anime.Id.ToString(), anime);

		return anime;
	}

	public async ValueTask<Anime?> GetAnime(string name)
	{
		if (!string.IsNullOrEmpty(name.Trim()) && s_cacheAnimeRaw.Exists(name)) return s_cacheAnimeRaw.Get(name);

		await m_firstLimit;

		var anime = (await GetAnime(name, 1))?.FirstOrDefault();
		if (anime != null)
			s_cacheAnimeRaw.Put(anime.Name, anime);

		return anime;
	}

	public async ValueTask<Anime[]?> GetAnime(string name, byte limit)
	{
		if (!string.IsNullOrEmpty(name.Trim()) && s_cacheAnimeRaw.Exists(name))
			return new[] { s_cacheAnimeRaw.Get(name) };

		await m_firstLimit;

		var anime = await m_shikimoriClient.Animes.GetAnime(new AnimeRequestSettings
		{
			search = name,
			limit = limit > 0 ? limit : 1
		});

		foreach (var a in anime)
			if (a != null)
				s_cacheAnimeRaw.Put(a.Name, a);

		return anime;
	}
}