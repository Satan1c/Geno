using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Geno.Types;
using MongoDB.Driver;

namespace Geno.Utils;

public static class DbExtensions
{
    public static async Task<GuildDocument> GetConfig(this IMongoCollection<GuildDocument> collection, ulong id) 
        => await collection.FindOrInsertOne(x => x.Id == id, new GuildDocument
        {
            Id = id
        });
    
    public static async Task SetConfig(this IMongoCollection<GuildDocument> collection, GuildDocument document) 
        => await collection.InsertOrReplaceOne(x => x.Id == document.Id, document);

    private static async Task InsertOrReplaceOne<TDocument>(
        this IMongoCollection<TDocument> collection,
        Expression<Func<TDocument, bool>> filter,
        TDocument document)
    {
        if (await collection.CountDocumentsAsync(filter) < 1)
            await collection.InsertOneAsync(document);
        else
            await collection.ReplaceOneAsync(filter, document);
    }
    
    private static async Task<TDocument> FindOrInsertOne<TDocument>(this IMongoCollection<TDocument> collection,
        Expression<Func<TDocument, bool>> filter, TDocument document)
        => (await collection.FindOrInsert(filter, document)).First();

    private static async Task<IAsyncCursor<TDocument>> FindOrInsert<TDocument>(
        this IMongoCollection<TDocument> collection,
        Expression<Func<TDocument, bool>> filter,
        TDocument document)
    {
        if (await collection.CountDocumentsAsync(filter) > 0) return await collection.FindAsync(filter);

        await collection.InsertOneAsync(document);
        return await collection.FindAsync(filter);
    }
}