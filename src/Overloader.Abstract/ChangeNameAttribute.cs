namespace Overloader;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class ChangeNameAttribute : Attribute
{
	public ChangeNameAttribute(string newName, Type? forType = null) { }
}
