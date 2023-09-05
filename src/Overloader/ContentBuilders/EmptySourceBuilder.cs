namespace Overloader.ContentBuilders;

public sealed class EmptySourceBuilder : SourceBuilder
{
	public static SourceBuilder Instance { get; } = new EmptySourceBuilder();
	protected override char this[int index] => throw new IndexOutOfRangeException();
	protected override int Length => 0;
	protected override void AppendCharToBuilder(char character) { }
	protected override void AppendStringToBuilder(string str) { }
	protected override SourceBuilder NewBuilder() => Instance;
	public override SourceBuilder CloneBuilder() => Instance;
	protected override void DisposeBuilder() { }
	protected override void ClearBuilder() { }
	public override string ToString() => string.Empty;
}
