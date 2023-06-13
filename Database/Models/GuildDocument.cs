using MongoDB.Bson.Serialization.Attributes;

namespace Database.Models;

public struct GuildDocument
{
	[BsonConstructor]
	public GuildDocument(ulong id = 0,
		Dictionary<string, ulong> voices = null!,
		Dictionary<string, string> voicesNames = null!,
		Dictionary<string, ulong> channels = null!,
		Dictionary<string, ulong[]> rankRoles = null!,
		Dictionary<string, ulong> userScreens = null!,
		bool forDeletion = false)
	{
		Id = id;
		Voices = voices ?? new Dictionary<string, ulong>();
		VoicesNames = voicesNames ?? new Dictionary<string, string>();
		Channels = channels ?? new Dictionary<string, ulong>();
		RankRoles = rankRoles ?? new Dictionary<string, ulong[]>();
		UserScreens = userScreens ?? new Dictionary<string, ulong>();
		ForDeletion = forDeletion;
	}

	[BsonElement("_id")] public ulong Id { get; set; }
	[BsonElement("voices")] public Dictionary<string, ulong> Voices { get; set; } = new();
	[BsonElement("voices_names")] public Dictionary<string, string> VoicesNames { get; set; } = new();
	[BsonElement("channels")] public Dictionary<string, ulong> Channels { get; set; } = new();
	[BsonElement("rank_roles")] public Dictionary<string, ulong[]> RankRoles { get; set; } = new();
	[BsonElement("users_screens")] public Dictionary<string, ulong> UserScreens { get; set; } = new();
	[BsonElement("for_deletion")] public bool ForDeletion { get; set; }

	public static GuildDocument GetDefault(ulong id)
	{
		return new GuildDocument(id);
	}
}