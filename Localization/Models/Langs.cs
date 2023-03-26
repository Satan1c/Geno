using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Localization.Models;

[JsonConverter(typeof(StringEnumConverter))]
public enum Langs
{
	[EnumMember(Value = "ru")]
	Ru,
	[EnumMember(Value = "en-GB")]
	En
}

public static class LangsExtensions
{
	public static string LangsToString(this Langs langs)
	{
		return langs switch
		{
			Langs.Ru => nameof(Langs.Ru),
			Langs.En => nameof(Langs.En),
			_ => throw new ArgumentOutOfRangeException(nameof(langs), langs, null)
		};
	}
}