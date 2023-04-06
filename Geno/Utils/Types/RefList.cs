using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Database;

namespace Geno.Utils.Types;

public ref struct RefList<T>
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
		m_buffer = list.ToArray().AsSpan();
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
			AutoResize(index + 1);
			m_buffer[index] = value;
		}
	}

	public T Add(T item)
	{
		AutoResize(Count + 1);

		m_buffer[Count++] = item;
		return item;
	}

	public T? FirstOrDefault(Func<T, bool> predicate)
	{
		for (var i = 0; i < Count; i++)
			if (predicate(m_buffer[i]))
				return m_buffer[i];

		return default;
	}

	public bool Contains(T item)
	{
		ref var start = ref MemoryMarshal.GetReference(m_buffer);
		for (var i = 0; i < Count; i++)
			if (Unsafe.Add(ref start, i).AreSame(item))
				return true;

		return false;
	}

	public Span<T> AsSpan()
	{
		return m_buffer.Slice(0, Count);
	}

	public T[] ToArray()
	{
		return m_buffer.Slice(0, Count).ToArray();
	}

	private void AutoResize(int index)
	{
		if (m_buffer.Length >= index) return;

		var resizer = new T[m_buffer.Length * 2].AsSpan();
		m_buffer.CopyTo(resizer);
		m_buffer = resizer;
	}
}