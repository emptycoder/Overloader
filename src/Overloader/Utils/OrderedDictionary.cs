using System.Collections;

namespace Overloader.Utils;

internal class OrderedDictionary<TKey, TValue>
{
	private readonly Dictionary<TKey, int> _indexes;
	private readonly TValue[] _values;

	public OrderedDictionary(int size)
	{
		_values = new TValue[size];
		_indexes = new Dictionary<TKey, int>(size);
	}

	public int Count { get; private set; }

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
