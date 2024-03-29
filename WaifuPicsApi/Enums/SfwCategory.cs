﻿using System.Runtime.Serialization;

namespace WaifuPicsApi.Enums;

public enum SfwCategory
{
	[EnumMember(Value = "waifu")] Waifu,
	[EnumMember(Value = "neko")] Neko,
	[EnumMember(Value = "shinobu")] Shinobu,
	[EnumMember(Value = "megumin")] Megumin,
	[EnumMember(Value = "bully")] Bully,
	[EnumMember(Value = "cuddle")] Cuddle,
	[EnumMember(Value = "cry")] Cry,
	[EnumMember(Value = "hug")] Hug,
	[EnumMember(Value = "awoo")] Awoo,
	[EnumMember(Value = "kiss")] Kiss,
	[EnumMember(Value = "lick")] Lick,
	[EnumMember(Value = "pat")] Pat,
	[EnumMember(Value = "smug")] Smug,
	[EnumMember(Value = "bonk")] Bonk,
	[EnumMember(Value = "yeet")] Yeet,
	[EnumMember(Value = "blush")] Blush,
	[EnumMember(Value = "smile")] Smile,
	[EnumMember(Value = "wave")] Wave,
	[EnumMember(Value = "highfive")] Highfive,
	[EnumMember(Value = "handhold")] Handhold,
	[EnumMember(Value = "nom")] Nom,
	[EnumMember(Value = "bite")] Bite,
	[EnumMember(Value = "glomp")] Glomp,
	[EnumMember(Value = "slap")] Slap,
	[EnumMember(Value = "kill")] Kill,
	[EnumMember(Value = "kick")] Kick,
	[EnumMember(Value = "happy")] Happy,
	[EnumMember(Value = "wink")] Wink,
	[EnumMember(Value = "poke")] Poke,
	[EnumMember(Value = "dance")] Dance,
	[EnumMember(Value = "cringe")] Cringe
}

public static class SfwCategoryExtension
{
	public static string CategoryToString(this SfwCategory category)
	{
		return category switch
		{
			SfwCategory.Waifu => nameof(SfwCategory.Waifu),
			SfwCategory.Neko => nameof(SfwCategory.Neko),
			SfwCategory.Shinobu => nameof(SfwCategory.Shinobu),
			SfwCategory.Megumin => nameof(SfwCategory.Megumin),
			SfwCategory.Bully => nameof(SfwCategory.Bully),
			SfwCategory.Cuddle => nameof(SfwCategory.Cuddle),
			SfwCategory.Cry => nameof(SfwCategory.Cry),
			SfwCategory.Hug => nameof(SfwCategory.Hug),
			SfwCategory.Awoo => nameof(SfwCategory.Awoo),
			SfwCategory.Kiss => nameof(SfwCategory.Kiss),
			SfwCategory.Lick => nameof(SfwCategory.Lick),
			SfwCategory.Pat => nameof(SfwCategory.Pat),
			SfwCategory.Smug => nameof(SfwCategory.Smug),
			SfwCategory.Bonk => nameof(SfwCategory.Bonk),
			SfwCategory.Yeet => nameof(SfwCategory.Yeet),
			SfwCategory.Blush => nameof(SfwCategory.Blush),
			SfwCategory.Smile => nameof(SfwCategory.Smile),
			SfwCategory.Wave => nameof(SfwCategory.Wave),
			SfwCategory.Highfive => nameof(SfwCategory.Highfive),
			SfwCategory.Handhold => nameof(SfwCategory.Handhold),
			SfwCategory.Nom => nameof(SfwCategory.Nom),
			SfwCategory.Bite => nameof(SfwCategory.Bite),
			SfwCategory.Glomp => nameof(SfwCategory.Glomp),
			SfwCategory.Slap => nameof(SfwCategory.Slap),
			SfwCategory.Kill => nameof(SfwCategory.Kill),
			SfwCategory.Kick => nameof(SfwCategory.Kick),
			SfwCategory.Happy => nameof(SfwCategory.Happy),
			SfwCategory.Wink => nameof(SfwCategory.Wink),
			SfwCategory.Poke => nameof(SfwCategory.Poke),
			SfwCategory.Dance => nameof(SfwCategory.Dance),
			SfwCategory.Cringe => nameof(SfwCategory.Cringe),
			_ => ""
		};
	}
}