namespace Overloader.ContentBuilders;

public sealed class EmptySourceBuilder : SourceBuilder
{
	public static SourceBuilder Instance { get; } = new EmptySourceBuilder();
	protected override char this[int index] => throw new IndexOutOfRangeException();
	protected override int Length => 0;
	protected override void AppendChar(char character) { }
	protected override void AppendString(string str) { }
	protected override SourceBuilder NewBuilder() => Instance;
	public override SourceBuilder CloneBuilder() => Instance;
	protected override void DisposeBuilder() { }
	protected override void ClearBuilder() { }
	public override string ToString() => string.Empty;
}
