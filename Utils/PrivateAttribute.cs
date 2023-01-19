namespace Geno.Utils;

[AttributeUsage(AttributeTargets.Class)]
public class PrivateAttribute : Attribute
{
	public PrivateAttribute(Category categories)
	{
		Categories = categories;
	}

	public Category Categories { get; }

}

[Flags]
public enum Category : byte
{
	None = 0,
	Genshin = 1 << 0,
	Images = 1 << 1,
	Admin = 1 << 2
}