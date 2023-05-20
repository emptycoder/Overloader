namespace Overloader;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class AllowForAttribute : Attribute
{
	public AllowForAttribute(Type? type = null, string? reason = null) { }
}
