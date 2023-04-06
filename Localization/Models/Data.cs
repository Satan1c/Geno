using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Discord;

namespace Localization.Models;

public readonly struct Data
{
	internal readonly Dictionary<Langs, Dictionary<string, string>> RowsData;

	internal Data(ref Data based, ref Span<Row> rows)
	{
		var baseData =
			Unsafe.IsNullRef(ref based)
				? new Dictionary<Langs, Dictionary<string, string>>
				{
					{ Langs.Ru, new Dictionary<string, string>() }, { Langs.En, new Dictionary<string, string>() }
				}
				: based.RowsData;
		var ru = new Dictionary<string, string>(baseData[Langs.Ru]);
		var en = new Dictionary<string, string>(baseData[Langs.En]);

		RowsData = DataFromRows(ref rows, ru, en);
	}

	internal Data(ref Span<Row> rows)
	{
		RowsData = DataFromRows(ref rows);
	}

	private static Dictionary<Langs, Dictionary<string, string>> DataFromRows(
		ref Span<Row> rows,
		Dictionary<string, string>? ru = null,
		Dictionary<string, string>? en = null)
	{
		ru ??= new Dictionary<string, string>();
		en ??= new Dictionary<string, string>();

		ref var start = ref MemoryMarshal.GetReference(rows);
		ref var end = ref Unsafe.Add(ref start, rows.Length);

		while (Unsafe.IsAddressLessThan(ref start, ref end))
		{
			var key = start.Key;
			ru[key] = start.Ru;
			en[key] = start.En;

			start = ref Unsafe.Add(ref start, 1);
		}

		return new Dictionary<Langs, Dictionary<string, string>>
		{
			{ Langs.Ru, ru },
			{ Langs.En, en }
		};
	}

	public IReadOnlyDictionary<string, string> GetForLocale(IInteractionContext context)
	{
		return GetForLocale(context.Interaction.UserLocale);
		//GetForLocale(JsonConvert.SerializeObject(context.Interaction.UserLocale));
	}

	private IReadOnlyDictionary<string, string> GetForLocale(string locale)
	{
		return GetForLocale(locale == "ru" ? Langs.Ru : Langs.En);
		//GetForLocale(GetLocale(locale));
	}

	private IReadOnlyDictionary<string, string> GetForLocale(Langs locale)
	{
		return RowsData[locale].AsReadOnly();
	}
}