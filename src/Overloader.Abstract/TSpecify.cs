// ReSharper disable InconsistentNaming
namespace Overloader;

[AttributeUsage(AttributeTargets.Class
                | AttributeTargets.Struct
                | AttributeTargets.Interface,
	AllowMultiple = true)]
public class TSpecify : Attribute
{
	public TSpecify(Type templateType, params string[] formatters) { }
}
