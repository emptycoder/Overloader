namespace Overloader;

[AttributeUsage(AttributeTargets.Class
                | AttributeTargets.Struct
                | AttributeTargets.Interface,
	AllowMultiple = true)]
// ReSharper disable once InconsistentNaming
public class TSpecifyAttribute : Attribute
{
	public TSpecifyAttribute(Type templateType, params string[] formatters) { }
}
