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
	Genshin = 0,
	Reactions = 1,
	Admin = 2
}