using System.Linq.Expressions;
using MongoDB.Driver;

namespace Geno.Database;

public class DatabaseProvider
{
    private readonly DatabaseCache m_cache;

    private readonly IMongoClient m_client;

    private readonly IMongoCollection<GuildDocument> m_guildConfigs;

    private readonly IMongoDatabase m_mainDb;

    public DatabaseProvider(IMongoClient client, DatabaseCache cache)
    {
        m_cache = cache;
        m_client = client;
        m_mainDb = m_client.GetDatabase("main");
        m_guildConfigs = m_mainDb.GetCollection<GuildDocument>("guilds");
    }

    public async Task<bool> HasDocument(ulong id)
    {
        if (m_cache.HasDocument(id))
            return true;

        if (await m_guildConfigs.CountDocumentsAsync(x => x.Id == id) < 1) return false;

        m_cache.SetDocument((await m_guildConfigs.FindAsync(x => x.Id == id)).First());
        return true;
    }

    public async Task<GuildDocument> GetConfig(ulong id)
    {
        if (m_cache.TryGetDocument(id, out var document))
            return document;

        m_cache.SetDocument(id, await FindOrInsertOne(m_guildConfigs, x => x.Id == id, new GuildDocument
        {
            Id = id
        }));

        return await GetConfig(id);
    }

    public async Task SetConfig(GuildDocument document)
    {
        m_cache.SetDocument(document);
        await InsertOrReplaceOne(m_guildConfigs, x => x.Id == document.Id, document);
    }

    private static async Task InsertOrReplaceOne<TDocument>(IMongoCollection<TDocument> collection,
        Expression<Func<TDocument, bool>> filter, TDocument document)
    {
        if (await collection.CountDocumentsAsync(filter) < 1)
            await collection.InsertOneAsync(document);
        else
            await collection.ReplaceOneAsync(filter, document);
    }

    private static async Task<TDocument> FindOrInsertOne<TDocument>(IMongoCollection<TDocument> collection,
        Expression<Func<TDocument, bool>> filter, TDocument document)
    {
        return (await FindOrInsert(collection, filter, document)).First();
    }

    private static async Task<IAsyncCursor<TDocument>> FindOrInsert<TDocument>(IMongoCollection<TDocument> collection,
        Expression<Func<TDocument, bool>> filter, TDocument document)
    {
        if (await collection.CountDocumentsAsync(filter) > 0) return await collection.FindAsync(filter);

        await collection.InsertOneAsync(document);
        return await collection.FindAsync(filter);
    }
}