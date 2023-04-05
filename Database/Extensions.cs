using CacheManager.Core;
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

	private static async ValueTask CreateDeletionIndex<TDocument>(this IMongoCollection<TDocument> collection,
		TimeSpan expirationTime,
		IndexKeysDefinition<TDocument> indexKeys,
		string name = "deletion_index")
	{
		var indexModel = new CreateIndexModel<TDocument>(indexKeys, new CreateIndexOptions
		{
			ExpireAfter = expirationTime,
			Name = name
		});

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
		FilterDefinition<TDocument> filterDefinition,
		TDocument document)
	{
		var item = await collection.FindOneAndReplaceAsync(filterDefinition, document);

		if (item == null)
			await collection.InsertOneAsync(document);
	}

	public static async ValueTask<TDocument> FindOrInsert<TDocument>(this IMongoCollection<TDocument> collection,
		FilterDefinition<TDocument> filterDefinition,
		TDocument document)
	{
		var item = await collection.Find(filterDefinition).FirstOrDefaultAsync();
		if (item != null)
			return item;

		await collection.InsertOneAsync(document);
		return await collection.Find(filterDefinition).FirstOrDefaultAsync();
	}
}