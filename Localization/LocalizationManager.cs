using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CsvHelper;
using Localization.Models;

namespace Localization;

public class LocalizationManager
{
	private readonly Dictionary<string, Category> m_categories;

	public LocalizationManager(string filesPath)
	{
		var categories = new Dictionary<string, Category>();
		
		var files = Directory.GetFiles(filesPath, "*.csv");
		if (files.Length > 0)
		{
			Load(ref files, ref categories);
		}
		else
		{
			var directories = Directory.GetDirectories(filesPath);

			ref var directory = ref MemoryMarshal.GetArrayDataReference(directories);
			ref var end = ref Unsafe.Add(ref directory, directories.Length);

			while (Unsafe.IsAddressLessThan(ref directory, ref end))
			{
				files = Directory.GetFiles(directory, "*.csv");
				Load(ref files, ref categories);

				directory = ref Unsafe.Add(ref directory, 1);
			}
		}
		
		m_categories = categories;
	}

	private static void Load(ref string[] filesPaths, ref Dictionary<string, Category> categories)
	{
		var filesPathSpan = filesPaths.AsSpan();
		ref var filesPath = ref MemoryMarshal.GetReference(filesPathSpan);
		ref var end = ref Unsafe.Add(ref filesPath, filesPathSpan.Length);

		while (Unsafe.IsAddressLessThan(ref filesPath, ref end))
		{
			var path = filesPath.Replace('\\', '/').AsSpan();
			
			var split = path[(path.LastIndexOf('/') + 1)..];
			
			var category = new string(split[..split.IndexOf('.')]);
			var name = new string(split[(split.IndexOf('.') + 1)..split.LastIndexOf('.')]);
			
			var file = File.ReadAllText(new string(path));
			var lines = new CsvReader(new StringReader(file), CultureInfo.InvariantCulture).GetRecords<Row>().ToArray().AsSpan();

			ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(categories, category, out var exists);
			if (exists)
				value.Add(ref name, ref lines);
			else
				value = new Category(ref name, ref lines);

			filesPath = ref Unsafe.Add(ref filesPath, 1);
		}
	}

	public Category GetCategory(string category)
	{
		ref var value = ref CollectionsMarshal.GetValueRefOrNullRef(m_categories, category);
		return Unsafe.IsNullRef(ref value)
			? throw new NullReferenceException($"{category} category not found, {string.Join(',', m_categories.Keys)}")
			: value;
	}
}