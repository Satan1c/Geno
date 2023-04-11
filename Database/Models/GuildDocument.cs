using MongoDB.Bson.Serialization.Attributes;

namespace Database.Models;

public class GuildDocument
{
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

	[BsonElement("_id")] public ulong Id { get; set; }
	[BsonElement("voices")] public Dictionary<string, ulong> Voices { get; set; }
	[BsonElement("voices_names")] public Dictionary<string, string> VoicesNames { get; set; }
	[BsonElement("channels")] public Dictionary<string, ulong> Channels { get; set; }
	[BsonElement("rank_roles")] public Dictionary<string, ulong[]> RankRoles { get; set; }
	[BsonElement("users_screens")] public Dictionary<string, ulong> UserScreens { get; set; }
	[BsonElement("for_deletion")] public bool ForDeletion { get; set; }

	public static GuildDocument GetDefault(ulong id)
	{
		var document = new GuildDocument();
		document.Id = id;
		return document;
	}
}