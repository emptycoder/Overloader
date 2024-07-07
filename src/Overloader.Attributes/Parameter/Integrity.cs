namespace Overloader;

[AttributeUsage(AttributeTargets.Parameter)]
public class Integrity : Attribute
{
	public const string TagName = nameof(Integrity);
}
