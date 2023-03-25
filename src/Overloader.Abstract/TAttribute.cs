namespace Overloader;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
// ReSharper disable once InconsistentNaming
public class TAttribute : Attribute
{
	public TAttribute(Type? newType = null, Type? forType = null) { }
}
