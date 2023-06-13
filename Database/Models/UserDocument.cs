using Database.Types;
using MongoDB.Bson.Serialization.Attributes;

namespace Database.Models;

[BsonIgnoreExtraElements]
public struct UserDocument
{
	[BsonConstructor]
	public UserDocument(ulong id = 0,
		string hoYoLabCookies = null!,
		bool forDeletion = false,
		Dailies enabledAutoDailies = default!)
	{
		HoYoLabCookies = hoYoLabCookies ?? string.Empty;
		EnabledAutoDailies = enabledAutoDailies.AreSame(default) ? new Dailies() : enabledAutoDailies;
		Id = id;
		ForDeletion = forDeletion;
	}

	public UserDocument()
	{
	}

	[BsonElement("_id")] public ulong Id { get; set; }
	[BsonElement("hoyolab_cookies")] public string HoYoLabCookies { get; set; } = string.Empty;
	[BsonElement("hoyolab_auto_dailies")] public Dailies EnabledAutoDailies { get; set; } = new();
	[BsonElement("for_deletion")] public bool ForDeletion { get; set; }

	public static UserDocument GetDefault(ulong id)
	{
		return new UserDocument(id);
	}
}