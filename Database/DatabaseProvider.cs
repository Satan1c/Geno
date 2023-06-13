using CacheManager.Core;
using Database.Models;
using MongoDB.Driver;

namespace Database;

public class DatabaseProvider
{
	private static readonly ICacheManager<GuildDocument> s_guildsCache = CacheFactory.Build<GuildDocument>(part => part
		.WithMicrosoftMemoryCacheHandle()
		.WithExpiration(ExpirationMode.Sliding, TimeSpan.FromHours(1)));

	private static readonly ICacheManager<UserDocument> s_usersCache = CacheFactory.Build<UserDocument>(part => part
		.WithMicrosoftMemoryCacheHandle()
		.WithExpiration(ExpirationMode.Sliding, TimeSpan.FromHours(1)));

	private readonly IMongoCollection<GuildDocument> m_guildConfigs;
	private readonly IMongoCollection<UserDocument> m_usersConfigs;

	public DatabaseProvider(IMongoClient client)
	{
		var mainDb = client.GetDatabase("main");
		m_guildConfigs = mainDb.GetCollection<GuildDocument>("guilds");
		m_usersConfigs = mainDb.GetCollection<UserDocument>("users");
	}

	public async ValueTask<bool> HasGuild(ulong id)
	{
		return await m_guildConfigs.HasDocument(
			s_guildsCache,
			Builders<GuildDocument>.Filter.Eq(document => document.Id, id),
			id).ConfigureAwait(false);
	}

	public async ValueTask<bool> HasUser(ulong id)
	{
		return await m_usersConfigs.HasDocument(
			s_usersCache,
			Builders<UserDocument>.Filter.Eq(document => document.Id, id),
			id).ConfigureAwait(false);
	}

	public async ValueTask<GuildDocument> GetConfig(ulong id)
	{
		return Get(id.ToString(), s_guildsCache, GuildDocument.GetDefault(id), await HasGuild(id));
	}

	public async ValueTask<UserDocument> GetUser(ulong id)
	{
		return Get(id.ToString(), s_usersCache, UserDocument.GetDefault(id), await HasUser(id));
	}

	public async Task<UserDocument[]> GetUsers(FilterDefinition<UserDocument> filterDefinition)
	{
		return (await m_usersConfigs.Find(filterDefinition).ToListAsync()).ToArray();
	}

	public async ValueTask SetConfig(GuildDocument document)
	{
		var before = await GetConfig(document.Id).ConfigureAwait(false);

		await Set(
			document,
			before,
			document.Id.ToString(),
			s_guildsCache,
			Builders<GuildDocument>.Filter.Eq(d => d.Id, document.Id),
			m_guildConfigs);
	}

	public async ValueTask SetUser(UserDocument document)
	{
		var before = await GetUser(document.Id).ConfigureAwait(false);

		await Set(
			document,
			before,
			document.Id.ToString(),
			s_usersCache,
			Builders<UserDocument>.Filter.Eq(d => d.Id, document.Id),
			m_usersConfigs);
	}

	private static T Get<T>(string cacheKey, ICacheManager<T> cacheManager, T deff, bool isPresent)
	{
		T item;
		if (isPresent)
		{
			item = cacheManager.Get(cacheKey);
		}
		else
		{
			item = deff;
			cacheManager.Put(cacheKey, item);
		}

		return item;
	}

	private static async ValueTask Set<T>(
		T document,
		T before,
		string cacheKey,
		ICacheManager<T> cacheManager,
		FilterDefinition<T> filter,
		IMongoCollection<T> collection)
	{
		if (document.AreSame(before))
			return;

		cacheManager.Put(cacheKey, document);

		await collection.InsertOrReplaceOne(
			filter,
			document).ConfigureAwait(false);
	}
}