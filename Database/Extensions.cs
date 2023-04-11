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

		var item = await collection.Find(filterDefinition).ToListAsync();
		if (item.Count < 1) return false;

		cacheManager.Put(itemId, item[0]);
		return true;
	}

	public static async ValueTask InsertOrReplaceOne<TDocument>(this IMongoCollection<TDocument> collection,
		FilterDefinition<TDocument> filterDefinition,
		TDocument document)
	{
		var item = await collection.FindOneAndReplaceAsync(filterDefinition, document);

		if (item.AreSame(default))
			await collection.InsertOneAsync(document);
	}
}