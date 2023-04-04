using MongoDB.Bson.Serialization.Attributes;

namespace Database.Models;

public struct GuildDocument
{
	public GuildDocument()
	{
		Id = 0;
	}

	[BsonElement("_id")] public ulong Id { get; set; }
	[BsonElement("voices")] public Dictionary<string, ulong> Voices { get; set; } = new();
	[BsonElement("voices_names")] public Dictionary<string, string> VoicesNames { get; set; } = new();
	[BsonElement("channels")] public Dictionary<string, ulong> Channels { get; set; } = new();
	[BsonElement("rank_roles")] public Dictionary<string, ulong[]> RankRoles { get; set; } = new();
	[BsonElement("users_screens")] public Dictionary<string, ulong> UserScreens { get; set; } = new();
	[BsonElement("for_deletion")] public bool ForDeletion { get; set; } = false;

	public static GuildDocument GetDefault(ulong id)
	{
		var document = new GuildDocument();
		document.Id = id;
		return document;
	}
}