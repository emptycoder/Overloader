namespace Overloader;

[AttributeUsage(AttributeTargets.Class
                | AttributeTargets.Struct
                | AttributeTargets.Interface,
	AllowMultiple = true)]
public class InvertedMode : Attribute
{
	public const string TagName = nameof(InvertedMode);
}
