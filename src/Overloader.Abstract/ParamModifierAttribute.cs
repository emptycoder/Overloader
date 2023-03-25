namespace Overloader;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true, Inherited = false)]
public class ParamModifierAttribute : Attribute
{
	public ParamModifierAttribute(string modifier, string? insteadOf = null, Type? formatterType = null) { }
}
