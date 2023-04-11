using Database.Types;
using MongoDB.Bson.Serialization.Attributes;

namespace Database.Models;

public class UserDocument
{
	public UserDocument(ulong id = 0, GenshinIds genshinIds = default, GenshinRegion defaultRegion = default)
	{
		Id = id;
		GenshinIds = genshinIds;
		DefaultRegion = defaultRegion;
		ForDeletion = false;
	}

	[BsonElement("_id")] public ulong Id { get; set; }
	[BsonElement("genshin_ids")] public GenshinIds GenshinIds { get; set; }
	[BsonElement("default_region")] public GenshinRegion DefaultRegion { get; set; }
	[BsonElement("for_deletion")] public bool ForDeletion { get; set; }
	public uint DefaultGenshinId => GetGenshinId(DefaultRegion);

	public static UserDocument GetDefault(ulong id)
	{
		var document = new UserDocument();
		document.Id = id;
		return document;
	}
	
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