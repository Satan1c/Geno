using Discord;
using Newtonsoft.Json;

namespace Localization.Models;

public struct Data
{
	private readonly IDictionary<Langs, IDictionary<string, string>> m_data = new Dictionary<Langs, IDictionary<string, string>>();
	
	public Data(Row[] rows)
	{
		m_data[Langs.Ru] = new Dictionary<string, string>();
		m_data[Langs.En] = new Dictionary<string, string>();
		
		foreach (var row in rows)
		{
			m_data[Langs.Ru][row.Key] = row.Ru.Replace("\\n", "\n");
			m_data[Langs.En][row.Key] = row.En.Replace("\\n", "\n");
		}
	}
	
	public IReadOnlyDictionary<string, string> GetForLocale(IInteractionContext context)
	{
		return GetForLocale(JsonConvert.SerializeObject(context.Interaction.UserLocale));
	}
	public IReadOnlyDictionary<string, string> GetForLocale(string locale)
	{
		return GetForLocale(GetLocale(locale));
	}
	public IReadOnlyDictionary<string, string> GetForLocale(Langs locale)
	{
		return m_data[locale].AsReadOnly();
	}
	
	private static Langs GetLocale(string locale)
	{
		var d = JsonConvert.DeserializeObject<Langs>(locale);
		return d;
	}
}