using Database.Types;
using MongoDB.Bson.Serialization.Attributes;

namespace Database.Models;

public class UserDocument : BaseDocument
{
	[BsonElement("genshin_ids")] public GenshinIds GenshinIds { get; set; }
	[BsonElement("default_region")] public GenshinRegion DefaultRegion { get; set; }
	public uint DefaultGenshinId => GetGenshinId(DefaultRegion);

	public uint GetGenshinId(GenshinRegion region)
	{
		return region switch
		{
			GenshinRegion.Na => GenshinIds.Na,
			GenshinRegion.Eu => GenshinIds.Eu,
			GenshinRegion.As => GenshinIds.As,
			_ => throw new ArgumentOutOfRangeException(nameof(region), region, null)
		};
	}
}