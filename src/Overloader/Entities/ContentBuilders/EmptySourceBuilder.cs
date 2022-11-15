namespace Overloader.Entities.ContentBuilders;

internal sealed class EmptySourceBuilder : SourceBuilder
{
	private EmptySourceBuilder() : base(null!, null!) { }
	public static SourceBuilder Instance { get; } = new EmptySourceBuilder();
	public override SourceBuilder AppendWoTrim(ReadOnlySpan<char> str, sbyte breakCount = 0) => this;
	public override void Dispose() { }
}
