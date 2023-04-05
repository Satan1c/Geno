using MongoDB.Bson.Serialization.Attributes;

namespace Database.Models;

public struct GuildDocument
{
	public GuildDocument()
	{
		Id = 0;
	}

	[BsonElement("_id")] public ulong Id { get; set; }
	[BsonElement("voices")] public Dictionary<string, ulong> Voices { get; set; } = default!;
	[BsonElement("voices_names")] public Dictionary<string, string> VoicesNames { get; set; } = default!;
	[BsonElement("channels")] public Dictionary<string, ulong> Channels { get; set; } = default!;
	[BsonElement("rank_roles")] public Dictionary<string, ulong[]> RankRoles { get; set; } = default!;
	[BsonElement("users_screens")] public Dictionary<string, ulong> UserScreens { get; set; } = default!;
	[BsonElement("for_deletion")] public bool ForDeletion { get; set; } = false;

	public static GuildDocument GetDefault(ulong id)
	{
		var document = new GuildDocument();
		document.Id = id;
		return document;
	}
}