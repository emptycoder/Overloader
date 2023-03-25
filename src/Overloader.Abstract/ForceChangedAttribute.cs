namespace Overloader;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class ForceChangedAttribute : Attribute
{
	public ForceChangedAttribute() { }
}
