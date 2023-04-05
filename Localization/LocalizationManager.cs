using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CsvHelper;
using Localization.Models;

namespace Localization;

public class LocalizationManager
{
	private readonly IDictionary<string, Category> m_categories = new Dictionary<string, Category>();

	public LocalizationManager(string filesPath)
	{
		var files = Directory.GetFiles(filesPath, "*.csv");
		if (files.Length > 0)
		{
			Load(files);
		}
		else
		{
			var directories = Directory.GetDirectories(filesPath);

			ref var directory = ref MemoryMarshal.GetArrayDataReference(directories);
			ref var end = ref Unsafe.Add(ref directory, directories.Length);

			while (Unsafe.IsAddressLessThan(ref directory, ref end))
			{
				files = Directory.GetFiles(directory, "*.csv");
				Load(files);

				directory = ref Unsafe.Add(ref directory, 1);
			}
		}
	}

	private void Load(string[] filesPaths)
	{
		ref var filesPath = ref MemoryMarshal.GetArrayDataReference(filesPaths);
		ref var end = ref Unsafe.Add(ref filesPath, filesPaths.Length);

		while (Unsafe.IsAddressLessThan(ref filesPath, ref end))
		{
			var path = filesPath.Replace('\\', '/');
			var split = path.Split('/')[^1].Split('.');
			var category = split[0];
			var name = split[1];
			var file = File.ReadAllText(path);
			var lines = new CsvReader(new StringReader(file), CultureInfo.InvariantCulture).GetRecords<Row>().ToArray();

			if (m_categories.TryGetValue(category, out var value))
				value.Add(name, lines);
			else
				m_categories[category] = new Category(name, lines);

			filesPath = ref Unsafe.Add(ref filesPath, 1);
		}
	}

	public Category GetCategory(string category)
	{
		return m_categories.TryGetValue(category, out var value) ? value : new Category();
	}
}