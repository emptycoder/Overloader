// ReSharper disable InconsistentNaming
// ReSharper disable UnusedParameter.Local
namespace Overloader;

[AttributeUsage(AttributeTargets.Class
                | AttributeTargets.Struct
                | AttributeTargets.Interface,
	AllowMultiple = true)]
public class TSpecify : Attribute
{
	public TSpecify(Type templateType, params string[] formatters) { }
	
	public TSpecify(Type[] templateTypes, params string[] formatters) { }
}
