namespace Overloader;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class ChangeModifierAttribute : Attribute
{
	public ChangeModifierAttribute(string modifier, string newModifier, Type? templateType = null) { }
}
