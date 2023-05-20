namespace Overloader;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true)]
// ReSharper disable once InconsistentNaming
public class T : Attribute
{
	public T(Type? newType = null, Type? forType = null) { }
}
