using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Localization.Models;

public readonly struct Category
{
	private readonly Dictionary<string, Data> m_data = new();

	internal Category(ref string name, ref Span<Row> rows)
	{
		Add(ref name, ref rows);
	}

	internal void Add(ref string name, ref Span<Row> rows)
	{
		ref var data = ref CollectionsMarshal.GetValueRefOrNullRef(m_data, name);
		m_data[name] = Unsafe.IsNullRef(ref data)
			? new Data(ref rows)
			: new Data(ref data, ref rows);
	}

	public Data GetDataFor(string name)
	{
		return m_data[name];
	}
}