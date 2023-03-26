using MongoDB.Bson.Serialization.Attributes;

namespace Database.Models;

public class GuildDocument
{
	[BsonElement("_id")] public ulong Id { get; set; }

	[BsonElement("voices")] public IDictionary<string, ulong> Voices { get; set; } = new Dictionary<string, ulong>();

	[BsonElement("channels")]
	public IDictionary<string, ulong> Channels { get; set; } = new Dictionary<string, ulong>();

	[BsonElement("rank_roles")] public Dictionary<string, ulong[]> RankRoles { get; set; } = new();

	[BsonElement("users_screens")] public Dictionary<string, ulong> UserScreens { get; set; } = new();
}