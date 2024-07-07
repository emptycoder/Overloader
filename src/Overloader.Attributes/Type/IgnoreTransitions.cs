namespace Overloader;

[AttributeUsage(AttributeTargets.Class
                | AttributeTargets.Struct
                | AttributeTargets.Interface,
	AllowMultiple = true)]
public class IgnoreTransitions : Attribute
{
	public const string TagName = nameof(IgnoreTransitions);
}
