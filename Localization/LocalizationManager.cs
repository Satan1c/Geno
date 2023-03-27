﻿using System.Globalization;
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
			Load(files);
		else
			//foreach (var d in Directory.GetDirectories(filesPath))
			foreach (var directory in Directory.GetDirectories(filesPath))
					Load(Directory.GetFiles(directory, "*.csv"));
	}

	private void Load(string[] filesPaths)
	{
		foreach (var path in filesPaths)
		{
			var split = path.Split('\\')[^1].Split('.');
			var category = split[0];
			var name = split[1];
			var file = File.ReadAllText(path);
			//var lines = CsvReader.ReadFromText(file).ToArray();
			var lines = new CsvReader(new StringReader(file), CultureInfo.InvariantCulture).GetRecords<Row>().ToArray();

			if (m_categories.TryGetValue(category, out var value))
				value.Add(name, lines);
			else
				m_categories[category] = new Category(name, lines);
		}
	}

	public Category GetCategory(string category)
	{
		return m_categories[category];
	}
}