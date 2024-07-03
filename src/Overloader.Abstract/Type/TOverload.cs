// ReSharper disable InconsistentNaming
// ReSharper disable UnusedParameter.Local
namespace Overloader;

[AttributeUsage(AttributeTargets.Class
                | AttributeTargets.Struct
                | AttributeTargets.Interface,
	AllowMultiple = true)]
public class TOverload : Attribute
{
	public TOverload(
		Type type,
		string? nameRegex = null,
		string? regexReplace = null,
		params string[] formatters) { }
	
	public TOverload(
		Type[] types,
		string? nameRegex = null,
		string? regexReplace = null,
		params string[] formatters) { }
}
