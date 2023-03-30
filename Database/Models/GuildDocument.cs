using MongoDB.Bson.Serialization.Attributes;

namespace Database.Models;

public class GuildDocument : BaseDocument
{
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

	protected override bool Equals(BaseDocument? other)
	{
		return Equals((GuildDocument) other);
	}

	public bool Equals(GuildDocument? other)
	{
		return base.Equals(other)
		       && other is not null
		       && Voices.Equals(other.Voices)
		       && VoicesNames.Equals(other.VoicesNames)
		       && Channels.Equals(other.Channels)
		       && RankRoles.Equals(other.RankRoles)
		       && UserScreens.Equals(other.UserScreens);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Id, Voices, VoicesNames, Channels, RankRoles, UserScreens);
	}
}