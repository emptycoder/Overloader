// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedParameter.Local
namespace Overloader;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true)]
public class TAttribute : Attribute
{
	public const string TagName = "T";
	
	public TAttribute(byte templateIndex = 0, Type? newType = null, Type? templateTypeFor = null) { }
}
