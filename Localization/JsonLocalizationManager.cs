using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Localization;

public class JsonLocalizationManager
{
	private readonly string m_basePath;
	private readonly Regex m_categoryRegex = new("\\w+.(?<category>\\w(?:-\\w)?).json", RegexOptions.Compiled | RegexOptions.Singleline);

	public JsonLocalizationManager(string basePath)
	{
		m_basePath = basePath;
	}
	
	private string[] GetAllFiles()
	{
		return Directory.GetFiles(m_basePath, "*.*.json", SearchOption.TopDirectoryOnly);
	}

	public IDictionary<string, string> GetValues(IList<string> key, string identifier)
	{
		var values = new Dictionary<string, string>();
		var files = GetAllFiles();

		foreach (var allFile in files)
		{
			var match = m_categoryRegex.Match(Path.GetFileName(allFile));

			if (!match.Success)
				continue;

			var key1 = match.Groups["category"].Value;
			using var reader1 = new StreamReader(allFile);
			using var reader2 = new JsonTextReader(reader1);

			//var token = string.Join(".", key.Select( (Func<string, string>)(x => "['" + x + "']") )) + "." + identifier;
			var select = key.Select(x => "['" + x + "']");
			var token = string.Join(".", select) + "." + identifier;
			var str = JObject.Load(reader2).SelectToken(token)?.ToString() ?? null;

			if (str == null)
				continue;

			values[key1] = str;
		}

		return values;
	}
}