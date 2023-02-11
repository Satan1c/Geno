namespace Geno.Utils.Services.Database;

public class DatabaseCache
{
	private readonly Dictionary<ulong, GuildDocument> m_guildDocuments = new();

	public bool HasDocument(ulong id)
	{
		return m_guildDocuments.ContainsKey(id);
	}

	public bool TryGetDocument(ulong id, out GuildDocument document)
	{
		document = null!;
		if (!m_guildDocuments.ContainsKey(id)) return false;

		document = m_guildDocuments[id];
		return true;
	}

	public void SetDocument(GuildDocument document)
	{
		SetDocument(document.Id, document);
	}

	public void SetDocument(ulong id, GuildDocument document)
	{
		m_guildDocuments[id] = document;
	}
}