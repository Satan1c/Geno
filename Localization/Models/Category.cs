namespace Localization.Models;

public readonly struct Category
{
	private readonly IDictionary<string, Data> m_data = new Dictionary<string, Data>();

	public Category(string name, Row[] rows)
	{
		Add(name, rows);
	}

	public void Add(string name, Row[] rows)
	{
		m_data[name] = new Data(rows);
	}

	public Data GetDataFor(string name)
	{
		return m_data[name];
	}
}