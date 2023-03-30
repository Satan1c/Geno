using MongoDB.Bson.Serialization.Attributes;

namespace Database.Models;

public abstract class BaseDocument
{
	[BsonElement("_id")] public ulong Id { get; set; }
	[BsonElement("for_deletion")] public bool ForDeletion { get; set; } = false;

	public static bool operator ==(BaseDocument? aBaseDocument, BaseDocument? bDocument)
	{
		return aBaseDocument?.Equals(bDocument)
		       ?? (aBaseDocument is null && bDocument is not null) || (aBaseDocument is not null && bDocument is null);
	}

	public static bool operator !=(BaseDocument? aBaseDocument, BaseDocument? bDocument)
	{
		return !(aBaseDocument == bDocument);
	}
	
	protected virtual bool Equals(BaseDocument? other)
	{
		return other is not null
		       && Id == other.Id;
	}

	public override bool Equals(object? obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		return obj.GetType() == this.GetType() && Equals((BaseDocument)obj);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Id);
	}
}