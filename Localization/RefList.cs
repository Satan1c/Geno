using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Localization;

internal ref struct RefList<T>
{
	private const int m_defaultCapacity = 8;
	private Span<T> m_buffer;
	
	public RefList()
	{
		m_buffer = new T[m_defaultCapacity].AsSpan();
		Count = 0;
	}

	public RefList(int capacity)
	{
		m_buffer = new T[capacity].AsSpan();
		Count = 0;
	}

	public RefList(IEnumerable<T> list)
	{
		m_buffer = list.ToArray();
		Count = m_buffer.Length;
	}
	
	public RefList(IReadOnlyCollection<T> list)
	{
		m_buffer = list.ToArray();
		Count = m_buffer.Length;
	}
	
	public int Count { get; private set; }

	public T this[int index]
	{
		get => Count < index ? m_buffer[Count - 1] : m_buffer[index];

		set
		{
			AutoResize(index);
			m_buffer[index] = value;
		}
	}
	
	public T Add(T item)
	{
		AutoResize(Count);
		
		m_buffer[Count++] = item;
		return item;
	}

	public T? FirstOrDefault(Func<T, bool> predicate)
	{
		for (var i = 0; i < Count; i++)
		{
			if (predicate(m_buffer[i]))
				return m_buffer[i];
		}

		return default;
	}

	public T[] ToArray()
	{
		return m_buffer[..Count].ToArray();
	}

	private void AutoResize(int index, int? volume = null)
	{
		if (m_buffer.Length >= index) return;
		
		var resizer = new T[m_buffer.Length * 2].AsSpan();
		m_buffer.CopyTo(resizer);
		m_buffer = resizer;
	}
}

internal static class Extensins
{
	public static KeyValuePair<TKey, TValue[]>[] ToArray<TKey, TValue>(this RefList<KeyValuePair<TKey, LinkedList<TValue>>> list)
	{
		var arr = new KeyValuePair<TKey, TValue[]>[list.Count];
		ref var start = ref MemoryMarshal.GetArrayDataReference(arr);
		ref var startList = ref MemoryMarshal.GetArrayDataReference(list.ToArray());
		ref var end = ref Unsafe.Add(ref start, arr.Length);

		while (Unsafe.IsAddressLessThan(ref start, ref end))
		{
			start = new KeyValuePair<TKey, TValue[]>(startList.Key, startList.Value.ToArray());
			
			start = ref Unsafe.Add(ref start, 1);
			startList = ref Unsafe.Add(ref startList, 1);
		}

		return arr;
	}
	
	public static bool ContainsKey<TKey, TValue>(this RefList<KeyValuePair<TKey, TValue>> list, TKey category)
	{
		for (var i = 0; i < list.Count; i++)
		{
			if (AreSame(list[i].Key, category))
				return true;
		}

		return false;
	}
	
	public static bool TryGetValue<TKey, TValue>(this RefList<KeyValuePair<TKey, TValue>> list, TKey category, out TValue value)
	{
		for (var i = 0; i < list.Count; i++)
		{
			if (!AreSame(list[i].Key, category)) continue;
			
			value = list[i].Value;
			return true;
		}

		value = default;
		return false;
	}

	public static bool ContainsValue<TKey, TValue>(this RefList<KeyValuePair<TKey, TValue>> list, TValue category)
	{
		for (var i = 0; i < list.Count; i++)
		{
			if (AreSame(list[i].Value, category))
				return true;
		}

		return false;
	}

	private static bool AreSame<T>(T left, T right)
	{
		return EqualityComparer<T>.Default.Equals(left, right);
	}
}