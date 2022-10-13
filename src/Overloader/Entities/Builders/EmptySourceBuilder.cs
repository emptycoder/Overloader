namespace Overloader.Entities.Builders;

internal sealed class EmptySourceBuilder : SourceBuilder
{
	private EmptySourceBuilder() : base(null!, null!) { }
	public override SourceBuilder AppendWoTrim(ReadOnlySpan<char> str, sbyte breakCount = 0) => this;
	public static SourceBuilder Instance { get; } = new EmptySourceBuilder();
	public override void Dispose() { }
}
