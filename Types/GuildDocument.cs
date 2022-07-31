using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace Geno.Types;

public class GuildDocument
{
    [BsonElement("_id")]
    public ulong Id { get; set; }
    
    [BsonElement("voices")]
    public IDictionary<string, ulong> Voices { get; set; } = new Dictionary<string, ulong>();
    
    [BsonElement("category_id")]
    public ulong CategoryId { get; set; }
    [BsonElement("voice_id")]
    public ulong VoiceId { get; set; }
}