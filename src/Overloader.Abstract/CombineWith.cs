// ReSharper disable UnusedParameter.Local
namespace Overloader;

[AttributeUsage(AttributeTargets.Parameter)]
public class CombineWith : Attribute
{
	public CombineWith(string parameterName) { }
}
