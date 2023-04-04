namespace Geno.Utils.Types;

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

public static class PrivateAttributeExtension
{
	public static string CategoryToString(this Category category)
	{
		return category switch
		{
			Category.None => nameof(Category.None),
			Category.Genshin => nameof(Category.Genshin),
			Category.Images => nameof(Category.Images),
			Category.Admin => nameof(Category.Admin),
			_ => ""
		};
	}
}