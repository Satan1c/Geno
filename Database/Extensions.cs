using System.Linq.Expressions;
using CacheManager.Core;
using Database.Models;
using MongoDB.Driver;

namespace Database;

public static class Extensions
{
	public static bool AreSame<T>(this T left, T right)
	{
		return EqualityComparer<T>.Default.Equals(left, right);
	}
	
	public static async ValueTask<bool> HasDocument<TDocument>(this IMongoCollection<TDocument> collection,
		ICacheManager<TDocument> cacheManager,
		FilterDefinition<TDocument> filterDefinition,
		ulong id)
	{
		var itemId = id.ToString();
		if (cacheManager.Exists(itemId))
			return true;
		
		var item = await collection.Find(filterDefinition).FirstOrDefaultAsync();
		if (item == null) return false;

		cacheManager.Put(itemId, item);
		return true;
	}

	public static async ValueTask CreateDeletionIndex<TDocument>(this IMongoCollection<TDocument> collection,
		TimeSpan expirationTime,
		IndexKeysDefinition<TDocument> indexKeys,
		string name = "deletion_index")
	{
		var indexOptions = new CreateIndexOptions
		{
			ExpireAfter = expirationTime,
			Name = name
		};
		var indexModel = new CreateIndexModel<TDocument>(indexKeys, indexOptions);

		await collection.Indexes.CreateOneAsync(indexModel);
	}

	public static async ValueTask RemoveIndex<TDocument>(this IMongoCollection<TDocument> collection,
		string name = "deletion_index")
	{
		await collection.Indexes.DropOneAsync(name);
	}

	public static async ValueTask ModifyIndex<TDocument>(this IMongoCollection<TDocument> collection,
		TimeSpan expirationTime,
		IndexKeysDefinition<TDocument> indexKeys,
		string name = "deletion_index",
		string? oldName = null)
	{
		await collection.RemoveIndex(oldName ?? name);
		await collection.CreateDeletionIndex(expirationTime, indexKeys, name);
	}

	public static async ValueTask InsertOrReplaceOne<TDocument>(this IMongoCollection<TDocument> collection,
		Expression<Func<TDocument, bool>> filter,
		TDocument document)
	{
		var item = await collection.FindOneAndReplaceAsync(filter, document);

		if (item == null)
			await collection.InsertOneAsync(document);
	}

	public static async ValueTask<TDocument> FindOrInsert<TDocument>(this IMongoCollection<TDocument> collection,
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