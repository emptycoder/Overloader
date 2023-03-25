namespace Overloader;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public class CombineWithAttribute : Attribute
{
	public CombineWithAttribute(string parameterName) { }
}
