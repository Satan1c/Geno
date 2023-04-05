using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Geno.Utils.Types;

[JsonConverter(typeof(StringEnumConverter))]
public enum UserLocales
{
	[EnumMember(Value = "en-GB")] English = 0,

	[EnumMember(Value = "ru")] Russian
}