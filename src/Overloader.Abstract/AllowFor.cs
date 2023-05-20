namespace Overloader;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class AllowFor : Attribute
{
	public AllowFor(Type? type = null, string? reason = null) { }
}
