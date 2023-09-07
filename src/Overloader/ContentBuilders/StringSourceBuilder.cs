using System.Text;
using Overloader.Compatibility;

namespace Overloader.ContentBuilders;

public sealed class StringSourceBuilder : SourceBuilder
{
	// ReSharper disable once RedundantSuppressNullableWarningExpression
	private static readonly ObjectPool<StringSourceBuilder> SPoolInstance = new(() =>
		new StringSourceBuilder(SPoolInstance!, new StringBuilder()), 40);

	private readonly StringBuilder _builder;
	private readonly ObjectPool<StringSourceBuilder> _pool;

	private StringSourceBuilder(ObjectPool<StringSourceBuilder> pool, StringBuilder builder) =>
		(_pool, _builder) = (pool, builder);

	protected override char this[int index] =>
		_builder[index];

	protected override int Length =>
		_builder.Length;

	public static StringSourceBuilder Instance =>
		SPoolInstance.Get() ??
		throw new ArgumentException("Can't allocate source builder instance.");

	protected override void AppendCharToBuilder(char character) =>
		_builder.Append(character);

	protected override void AppendStringToBuilder(string str) =>
		_builder.Append(str);

	public override string ToString() =>
		_builder.ToString();

	protected override SourceBuilder NewBuilder() =>
		Instance;

	protected override void DisposeBuilder() =>
		_pool.Return(this);

	protected override void ClearBuilder() =>
		_builder.Clear();

	public override SourceBuilder CloneBuilder()
	{
		var newInstance = Instance;
		newInstance._builder.Append(_builder);
		return newInstance;
	}
}
