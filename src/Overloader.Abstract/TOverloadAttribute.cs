namespace Overloader;

[AttributeUsage(AttributeTargets.Class
                | AttributeTargets.Struct
                | AttributeTargets.Interface,
	AllowMultiple = true)]
// ReSharper disable once InconsistentNaming
public class TOverloadAttribute : Attribute
{
	public TOverloadAttribute(Type? type = null, string? nameRegex = null, string? regexReplace = null, params string[] formatters) { }
}
