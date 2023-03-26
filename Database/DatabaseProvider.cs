using System.Linq.Expressions;
using CacheManager.Core;
using Database.Models;
using MongoDB.Driver;

namespace Database;

public class DatabaseProvider
{
	private static readonly ICacheManager<GuildDocument> s_cache = CacheFactory.Build<GuildDocument>(part =>
		part.WithMicrosoftMemoryCacheHandle().WithExpiration(ExpirationMode.Sliding, TimeSpan.FromHours(1)));

	private readonly IMongoCollection<GuildDocument> m_guildConfigs;

	public DatabaseProvider(IMongoClient client)
	{
		var mainDb = client.GetDatabase("main");
		m_guildConfigs = mainDb.GetCollection<GuildDocument>("guilds");
	}

	public async Task<bool> HasDocument(ulong id)
	{
		var itemId = id.ToString();
		if (s_cache.Exists(itemId))
			return true;

		var item = await m_guildConfigs.Find(x => x.Id == id).FirstOrDefaultAsync();
		if (item == null) return false;

		s_cache.Put(itemId, item);
		return true;
	}

	public async Task<GuildDocument> GetConfig(ulong id)
	{
		var itemId = id.ToString();
		if (s_cache.Exists(itemId))
			return s_cache.Get(itemId);

		var item = await FindOrInsert(m_guildConfigs, x => x.Id == id, new GuildDocument
		{
			Id = id
		});
		s_cache.Put(itemId, item);

		return await GetConfig(id);
	}

	public Task SetConfig(GuildDocument document)
	{
		s_cache.Put(document.Id.ToString(), document);
		return InsertOrReplaceOne(m_guildConfigs, x => x.Id == document.Id, document);
	}

	private static async Task InsertOrReplaceOne<TDocument>(IMongoCollection<TDocument> collection,
		Expression<Func<TDocument, bool>> filter, TDocument document)
	{
		var item = await collection.FindOneAndReplaceAsync(filter, document);

		if (item == null)
			await collection.InsertOneAsync(document);
	}

	private static async Task<TDocument> FindOrInsert<TDocument>(IMongoCollection<TDocument> collection,
		Expression<Func<TDocument, bool>> filter, TDocument document)
	{
		var item = await collection.Find(filter).FirstOrDefaultAsync();
		if (item != null)
			return item;

		await collection.InsertOneAsync(document);
		return await collection.Find(filter).FirstOrDefaultAsync();
	}
}