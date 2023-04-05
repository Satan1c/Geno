using System.Runtime.Serialization;

namespace WaifuPicsApi.Enums;

public enum Type : byte
{
	[EnumMember(Value = "sfw")] Sfw,
	[EnumMember(Value = "nsfw")] Nsfw
}