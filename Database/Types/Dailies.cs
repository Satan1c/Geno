using MongoDB.Bson.Serialization.Attributes;

namespace Database.Types;

public struct Dailies
{
	public bool Hsr { get; set; }
	public bool Genshin { get; set; }

	[BsonConstructor]
	public Dailies(bool hsr = false, bool genshin = false)
	{
		Hsr = hsr;
		Genshin = genshin;
	}
}