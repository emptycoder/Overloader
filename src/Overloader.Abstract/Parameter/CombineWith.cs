// ReSharper disable UnusedParameter.Local
namespace Overloader;

[AttributeUsage(AttributeTargets.Parameter)]
public class CombineWith : Attribute
{
	public const string TagName = nameof(CombineWith);
	
	public CombineWith(string parameterName) { }
}
