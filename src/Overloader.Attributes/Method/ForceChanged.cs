namespace Overloader;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class ForceChanged : Attribute
{
	public const string TagName = nameof(ForceChanged);
}
