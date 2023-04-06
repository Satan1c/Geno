using MongoDB.Bson.Serialization.Attributes;

namespace Database.Models;

public struct GuildDocument
{
	public GuildDocument()
	{
	}

	[BsonConstructor]
	public GuildDocument(ulong id = 0,
		Dictionary<string, ulong>? voices = default!,
		Dictionary<string, string>? voicesNames = default!,
		Dictionary<string, ulong>? channels = default!,
		Dictionary<string, ulong[]>? rankRoles = default!,
		Dictionary<string, ulong>? userScreens = default!,
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

	[BsonElement("_id")] public ulong Id { get; set; } = 0;
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