using System.Text;
using Overloader.Utils;

namespace Overloader.Entities;

internal sealed class SourceBuilder : IDisposable
{
	private const string PaddingStr = "\t";
	private const string NestedUpStr = "{";
	private const string NestedDownStr = "}";

	// ReSharper disable once RedundantSuppressNullableWarningExpression
	private static readonly ObjectPool<SourceBuilder> SPoolInstance = new(() => new SourceBuilder(SPoolInstance!), 16);

	private readonly StringBuilder _builder = new();
	private readonly ObjectPool<SourceBuilder> _pool;
	private uint _nestedLevel;
	private bool _nextLine = true;

	private SourceBuilder(ObjectPool<SourceBuilder> pool) => _pool = pool;

	public void Dispose()
	{
		Clear();
		_pool.Free(this);
	}

	public SourceBuilder Append(string? str, sbyte breakCount = 0) => AppendWoTrim(str.AsSpan().Trim(), breakCount);

	public SourceBuilder Append(ReadOnlySpan<char> str, sbyte breakCount = 0) => AppendWoTrim(str.Trim(), breakCount);
	public SourceBuilder AppendWoTrim(string? str, sbyte breakCount = 0) => AppendWoTrim(str.AsSpan(), breakCount);

	public SourceBuilder AppendWoTrim(ReadOnlySpan<char> str, sbyte breakCount = 0)
	{
		if (_nextLine)
		{
			for (int index = 0; index < _nestedLevel; index++)
				_builder.Append(PaddingStr);
			_nextLine = false;
		}

		bool isNextLineFound = false;
		for (int index = 0; index < str.Length; index++)
		{
			char character = str[index];
			_builder.Append(character);
			isNextLineFound |= character is '\n';
		}

		if (breakCount <= 0 && !isNextLineFound) return this;

		for (int index = 0; index < breakCount; index++)
			_builder.Append('\n');
		_nextLine = true;

		return this;
	}

	public SourceBuilder AppendWith(string? str, string separator) => Append(str).AppendWoTrim(separator);

	public SourceBuilder NestedIncrease()
	{
		Append(NestedUpStr, 1);
		_nestedLevel++;

		return this;
	}

	public SourceBuilder NestedDecrease()
	{
		if (_nestedLevel == 0)
			throw new Exception("Minimum nested level has been reached");

		_nestedLevel--;
		Append(NestedDownStr, 1);

		return this;
	}

	public void Clear()
	{
		_nextLine = true;
		_nestedLevel = 0;
		_builder.Clear();
	}

	public override string ToString() => _builder.ToString();

	public string ToStringAndClear()
	{
		var result = _builder.ToString();
		Clear();
		return result;
	}

	public string ToStringAndDispose()
	{
		string result = _builder.ToString();
		Dispose();
		return result;
	}

	public SourceBuilder GetDependentInstance()
	{
		var instance = GetInstance();
		instance._nestedLevel = _nestedLevel;

		return instance;
	}

	public SourceBuilder GetClonedInstance()
	{
		var newInstance = GetInstance();
		newInstance._builder.Append(_builder);
		return newInstance;
	}

	public static SourceBuilder GetInstance() => SPoolInstance.Allocate() ?? throw new ArgumentException(
		"Can't allocate source builder instance.");
}
