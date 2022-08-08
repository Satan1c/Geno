using MongoDB.Bson.Serialization.Attributes;

namespace Geno.Database;

public class GuildDocument
{
    [BsonElement("_id")] public ulong Id { get; set; }

    [BsonElement("voices")] public IDictionary<string, ulong> Voices { get; set; } = new Dictionary<string, ulong>();

    [BsonElement("channels")]
    public IDictionary<string, ulong> Channels { get; set; } = new Dictionary<string, ulong>();
}