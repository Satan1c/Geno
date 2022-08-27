namespace Geno.Database;

public class DatabaseCache
{
    private Dictionary<ulong, GuildDocument> m_guildDocuments = new();

    public DatabaseCache()
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromHours(6));

            m_guildDocuments = new Dictionary<ulong, GuildDocument>();
        });
    }

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