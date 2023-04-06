using Database.Types;
using MongoDB.Bson.Serialization.Attributes;

namespace Database.Models;

public struct UserDocument
{
	public UserDocument()
	{
	}
	
	public UserDocument(ulong id, GenshinIds genshinIds, GenshinRegion defaultRegion)
	{
		Id = id;
		GenshinIds = genshinIds;
		DefaultRegion = defaultRegion;
		ForDeletion = false;
	}

	[BsonElement("_id")] public ulong Id { get; set; } = 0;
	[BsonElement("genshin_ids")] public GenshinIds GenshinIds { get; set; } = default;
	[BsonElement("default_region")] public GenshinRegion DefaultRegion { get; set; } = default;
	[BsonElement("for_deletion")] public bool ForDeletion { get; set; } = false;
	public uint DefaultGenshinId => GetGenshinId(DefaultRegion);

	public uint GetGenshinId(GenshinRegion region)
	{
		return region switch
		{
			GenshinRegion.Na => GenshinIds.Na,
			GenshinRegion.Eu => GenshinIds.Eu,
			GenshinRegion.As => GenshinIds.As,
			_ => GenshinIds.Eu
		};
	}
}