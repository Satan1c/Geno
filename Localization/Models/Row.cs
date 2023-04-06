namespace Localization.Models;

public readonly struct Row
{
	private readonly string m_ru;
	private readonly string m_en;
	public string Key { get; init; }

	public string Ru
	{
		get => m_ru;
		init => m_ru = value.Replace("\\n", "\n");
	}

	public string En
	{
		get => m_en;
		init => m_en = value.Replace("\\n", "\n");
	}
}