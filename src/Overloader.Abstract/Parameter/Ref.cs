namespace Overloader;

[AttributeUsage(AttributeTargets.Parameter)]
public class Ref : Attribute
{
	public const string TagName = nameof(Ref);
}
