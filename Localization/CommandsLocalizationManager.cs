using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Discord.Interactions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Localization;

public sealed class CommandsLocalizationManager : ILocalizationManager
{
	/*private const string m_nameIdentifier = "name";
	private const string m_descriptionIdentifier = "description";
	private const string m_spaceToken = "~";*/
	private readonly string m_basePath;

	private readonly Regex m_localeParserRegex = new(@"\w+.(?<locale>\w+(?:-\w+)?).json",
		RegexOptions.Compiled | RegexOptions.Singleline);

	public CommandsLocalizationManager(string basePath)
	{
		m_basePath = basePath;
	}

	public IDictionary<string, string> GetAllDescriptions(
		IList<string> key,
		LocalizationTarget destinationType)
	{
		return GetValues(key, "description");
	}

	public IDictionary<string, string> GetAllNames(
		IList<string> key,
		LocalizationTarget destinationType)
	{
		return GetValues(key, "name");
	}

	private string[] GetAllFiles()
	{
		return Directory.GetFiles(m_basePath, "*.*.json", SearchOption.TopDirectoryOnly);
	}

	private IDictionary<string, string> GetValues(IList<string> key, string identifier)
	{
		//var values = new Dictionary<string, string>();
		var dict = new RefList<KeyValuePair<string, string>>();
		var files = Directory.GetFiles(m_basePath, "*.*.json", SearchOption.TopDirectoryOnly);

		ref var start = ref MemoryMarshal.GetArrayDataReference(files);
		ref var end = ref Unsafe.Add(ref start, files.Length);

		while (Unsafe.IsAddressLessThan(ref start, ref end))
		{
			var match = m_localeParserRegex.Match(Path.GetFileName(start));

			if (match.Success)
			{
				var key1 = match.Groups["locale"].Value;
				using var reader1 = new StreamReader(start);
				using var reader2 = new JsonTextReader(reader1);

				//var token = string.Join(".", key.Select( (Func<string, string>)(x => "['" + x + "']") )) + "." + identifier;
				var select = key.Select(x => "['" + x + "']");
				var token = string.Join(".", select) + "." + identifier;
				var str = JObject.Load(reader2).SelectToken(token)?.ToString() ?? null;

				if (str != null)
					dict.Add(new KeyValuePair<string, string>(key1, str));
			}
			
			start = ref Unsafe.Add(ref start, 1);
		}

		return new Dictionary<string, string>(dict.ToArray());
	}
}