using System.Collections;

namespace Overloader.Entities;

public class OrderedDictionary<TKey, TValue>
{
	public static readonly OrderedDictionary<TKey, TValue> Empty = new(0);

	private readonly TValue[] _values;
	private readonly Dictionary<TKey, int> _indexes;

	public int Count { get; private set; }

	public OrderedDictionary(int size)
	{
		_values = new TValue[size];
		_indexes = new Dictionary<TKey, int>(size);
	}

	public void Add(TKey key, TValue value)
	{
		_indexes.Add(key, Count);
		_values[Count++] = value;
	}

	public Enumerator GetEnumerator() => new(this);

	public struct Enumerator : IEnumerator<(TKey Key, TValue Value)>
	{
		private readonly TValue[] _values;
		private Dictionary<TKey, int>.Enumerator _dictEnumerator;
		
		public Enumerator(OrderedDictionary<TKey, TValue> dict)
		{
			_values = dict._values;
			_dictEnumerator = dict._indexes.GetEnumerator();
		}

		public bool MoveNext() => _dictEnumerator.MoveNext();

		public void Reset() => throw new NotSupportedException();

		public (TKey Key, TValue Value) Current
		{
			get
			{
				var current = _dictEnumerator.Current;
				return (current.Key, _values[current.Value]);
			}
		}

		object IEnumerator.Current => Current;
		public void Dispose() => _dictEnumerator.Dispose();
	}
}
