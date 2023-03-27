using MongoDB.Bson.Serialization.Attributes;

namespace Database.Models;

public class GuildDocument
{
	[BsonElement("_id")] public ulong Id { get; set; }

	[BsonElement("voices")] public Dictionary<string, ulong> Voices { get; set; } = new();
	[BsonElement("voices_names")] public Dictionary<string, string> VoicesNames { get; set; } = new();
	[BsonElement("channels")] public Dictionary<string, ulong> Channels { get; set; } = new();
	[BsonElement("rank_roles")] public Dictionary<string, ulong[]> RankRoles { get; set; } = new();
	[BsonElement("users_screens")] public Dictionary<string, ulong> UserScreens { get; set; } = new();
	
	public static GuildDocument GetDefault(ulong id)
	{
		var document = new GuildDocument();
		document.Id = id;
		return document;
	}

	public static bool operator ==(GuildDocument? aGuildDocument, GuildDocument? bDocument)
	{
		return (aGuildDocument is null && bDocument is null) ||
		       aGuildDocument is not null && bDocument is not null &&
		       aGuildDocument.Id == bDocument.Id &&
		       aGuildDocument.Voices.SequenceEqual(bDocument.Voices) &&
		       aGuildDocument.VoicesNames.SequenceEqual(bDocument.VoicesNames) &&
		       aGuildDocument.Channels.SequenceEqual(bDocument.Channels) &&
		       aGuildDocument.RankRoles.SequenceEqual(bDocument.RankRoles) &&
		       aGuildDocument.UserScreens.SequenceEqual(bDocument.UserScreens);
	}

	public static bool operator !=(GuildDocument? aGuildDocument, GuildDocument? bDocument)
	{
		return !(aGuildDocument == bDocument);
	}
}