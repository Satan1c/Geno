using CacheManager.Core;
using MongoDB.Driver;

namespace Database;

public static class Extensions
{
	public static bool AreSame<T>(this T left, T right)
	{
		return EqualityComparer<T>.Default.Equals(left, right);
	}

	/*public static async ValueTask<bool> HasDocument<TDocument>(this IMongoCollection<TDocument> collection,
		ICacheManager<TDocument> cacheManager,
		FilterDefinition<TDocument> filterDefinition,
		ulong id)
	{
		var itemId = id.ToString();
		if (cacheManager.Exists(itemId))
			return true;

		var item = await collection.Find(filterDefinition).ToListAsync().ConfigureAwait(false);
		if (item.Count < 1) return false;

		cacheManager.Put(itemId, item[0]);
		return await collection.HasDocument(cacheManager, filterDefinition, itemId).ConfigureAwait(false);
	}*/

	public static async ValueTask<bool> HasDocument<TDocument>(this IMongoCollection<TDocument> collection,
		ICacheManager<TDocument> cacheManager,
		FilterDefinition<TDocument> filterDefinition,
		ulong id)
	{
		var itemId = id!.ToString();
		if (cacheManager.Exists(itemId))
			return true;

		List<TDocument> item;

		try
		{
			item = await collection.Find(filterDefinition).ToListAsync().ConfigureAwait(false);
		}
		catch
		{
			return false;
		}

		if (item.Count < 1) return false;

		cacheManager.Put(itemId, item[0]);
		return true;
	}

	public static async ValueTask InsertOrReplaceOne<TDocument>(this IMongoCollection<TDocument> collection,
		FilterDefinition<TDocument> filterDefinition,
		TDocument document)
	{
		var item = await collection.FindOneAndReplaceAsync(filterDefinition, document).ConfigureAwait(false);

		if (item.AreSame(default))
			await collection.InsertOneAsync(document).ConfigureAwait(false);
	}
}