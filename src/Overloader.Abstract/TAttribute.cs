// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming
namespace Overloader;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true)]
public class TAttribute : Attribute
{
	public const string TagName = "T";
	public TAttribute(Type? newType = null, Type? forType = null) { }
}
