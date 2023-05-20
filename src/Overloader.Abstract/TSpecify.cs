namespace Overloader;

[AttributeUsage(AttributeTargets.Class
                | AttributeTargets.Struct
                | AttributeTargets.Interface,
	AllowMultiple = true)]
// ReSharper disable once InconsistentNaming
public class TSpecify : Attribute
{
	public TSpecify(Type templateType, params string[] formatters) { }
}
