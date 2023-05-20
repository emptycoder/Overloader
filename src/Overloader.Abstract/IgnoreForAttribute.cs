namespace Overloader;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class IgnoreForAttribute : Attribute
{
	public IgnoreForAttribute(Type? type = null, string? reason = null) { }
}
