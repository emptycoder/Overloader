namespace Overloader;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
public class ParamModifier : Attribute
{
	public ParamModifier(string modifier, string? insteadOf = null, Type? formatterType = null) { }
}
