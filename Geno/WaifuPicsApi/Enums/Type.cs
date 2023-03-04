using System.Runtime.Serialization;

namespace Geno.WaifuPicsApi.Enums;

public enum Type : byte
{
	[EnumMember(Value = "sfw")] Sfw,
	[EnumMember(Value = "nsfw")] Nsfw
}