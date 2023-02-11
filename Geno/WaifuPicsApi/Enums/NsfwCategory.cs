using System.Runtime.Serialization;

namespace WaifuPicsApi.Enums;

public enum NsfwCategory
{
	[EnumMember(Value = "waifu")] Waifu,
	[EnumMember(Value = "neko")] Neko,
	[EnumMember(Value = "trap")] Trap,
	[EnumMember(Value = "blowjob")] Blowjob
}