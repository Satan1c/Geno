using System.Linq.Expressions;
using CacheManager.Core;
using Database.Models;
using MongoDB.Driver;

namespace Database;

public static class Extensions
{
	public static async Task<bool> HasDocument<TDocument>(this IMongoCollection<TDocument> collection,
		ICacheManager<TDocument> cacheManager,
		ulong id)
		where TDocument : BaseDocument
	{
		var itemId = id.ToString();
		if (cacheManager.Exists(itemId))
			return true;

		var item = await collection.Find(x => x.Id == id).FirstOrDefaultAsync();
		if (item == null) return false;

		cacheManager.Put(itemId, item);
		return true;
	}

	public static async Task CreateDeletionIndex<TDocument>(this IMongoCollection<TDocument> collection,
		TimeSpan expirationTime,
		string name = "deletion_index")
		where TDocument : BaseDocument
	{
		var indexKeys = Builders<TDocument>.IndexKeys.Ascending(x => x.ForDeletion);
		var indexOptions = new CreateIndexOptions
		{
			ExpireAfter = expirationTime,
			Name = name
		};
		var indexModel = new CreateIndexModel<TDocument>(indexKeys, indexOptions);

		await collection.Indexes.CreateOneAsync(indexModel);
	}

	public static async Task RemoveIndex<TDocument>(this IMongoCollection<TDocument> collection,
		string name = "deletion_index")
	{
		await collection.Indexes.DropOneAsync(name);
	}

	public static async Task ModifyIndex<TDocument>(this IMongoCollection<TDocument> collection,
		TimeSpan expirationTime,
		string name = "deletion_index",
		string? oldName = null)
		where TDocument : BaseDocument
	{
		await collection.RemoveIndex(oldName ?? name);
		await collection.CreateDeletionIndex(expirationTime, name);
	}

	public static async Task InsertOrReplaceOne<TDocument>(this IMongoCollection<TDocument> collection,
		Expression<Func<TDocument, bool>> filter,
		TDocument document)
	{
		var item = await collection.FindOneAndReplaceAsync(filter, document);

		if (item == null)
			await collection.InsertOneAsync(document);
	}

	public static async Task<TDocument> FindOrInsert<TDocument>(this IMongoCollection<TDocument> collection,
		Expression<Func<TDocument, bool>> filter,
		TDocument document)
	{
		var item = await collection.Find(filter).FirstOrDefaultAsync();
		if (item != null)
			return item;

		await collection.InsertOneAsync(document);
		return await collection.Find(filter).FirstOrDefaultAsync();
	}
}