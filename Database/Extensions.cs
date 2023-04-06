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
		where TDocument : struct
	{
		var itemId = id.ToString();
		if (cacheManager.Exists(itemId))
			return true;

		var item = await collection.Find(filterDefinition).FirstOrDefaultAsync();
		if (item.AreSame(default)) return false;

		cacheManager.Put(itemId, item);
		return true;
	}

	public static async ValueTask InsertOrReplaceOne<TDocument>(this IMongoCollection<TDocument> collection,
		FilterDefinition<TDocument> filterDefinition,
		TDocument document)
		where TDocument : struct
	{
		var item = await collection.FindOneAndReplaceAsync(filterDefinition, document);

		if (item.AreSame(default))
			await collection.InsertOneAsync(document);
	}
}