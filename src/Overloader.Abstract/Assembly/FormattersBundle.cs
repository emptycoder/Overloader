namespace Overloader;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class FormattersBundle : Attribute
{
	public const string TagName = nameof(FormattersBundle);
	
	public FormattersBundle(string bundleName, params string[] formatterNames) { }
}
