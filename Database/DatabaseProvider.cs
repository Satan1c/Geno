using CacheManager.Core;
using Database.Models;
using MongoDB.Driver;

namespace Database;

public class DatabaseProvider
{
	private static readonly ICacheManager<GuildDocument> s_guildsCache = CacheFactory.Build<GuildDocument>(part => part
		.WithMicrosoftMemoryCacheHandle()
		.WithExpiration(ExpirationMode.Sliding, TimeSpan.FromDays(7)));

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

	public ValueTask<bool> HasGuild(ulong id)
	{
		return m_guildConfigs.HasDocument(
			s_guildsCache,
			Builders<GuildDocument>.Filter.Eq(document => document.Id, id),
			id);
	}

	public async ValueTask<bool> HasUser(ulong id)
	{
		return await m_usersConfigs.HasDocument(
			s_usersCache,
			Builders<UserDocument>.Filter.Eq(document => document.Id, id),
			id);
	}

	public async ValueTask<GuildDocument> GetConfig(ulong id)
	{
		GuildDocument item;
		var itemId = id.ToString();
		if (await HasGuild(id))
		{
			item = s_guildsCache.Get(itemId);
		}
		else
		{
			item = GuildDocument.GetDefault(id);
			s_guildsCache.Put(itemId, item);
		}

		return item;
	}

	public async ValueTask SetConfig(GuildDocument document, GuildDocument? before = null)
	{
		before ??= await GetConfig(document.Id);

		if (document.AreSame(before))
			return;

		s_guildsCache.Put(document.Id.ToString(), document);

		await m_guildConfigs.InsertOrReplaceOne(Builders<GuildDocument>.Filter.Eq(d => d.Id, document.Id), document);
	}
}