namespace WaifuPicsApi.Enums;

public static class Extensions
{
	public static string EnumToString(this Type type)
	{
		return type switch
		{
			Type.Sfw => nameof(Type.Sfw),
			Type.Nsfw => nameof(Type.Nsfw),
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};
	}

	public static string EnumToString(this SfwCategory category)
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
			_ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
		};
	}

	public static string EnumToString(this NsfwCategory category)
	{
		return category switch
		{
			NsfwCategory.Waifu => nameof(NsfwCategory.Waifu),
			NsfwCategory.Neko => nameof(NsfwCategory.Neko),
			NsfwCategory.Trap => nameof(NsfwCategory.Trap),
			NsfwCategory.Blowjob => nameof(NsfwCategory.Blowjob),
			_ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
		};
	}
}