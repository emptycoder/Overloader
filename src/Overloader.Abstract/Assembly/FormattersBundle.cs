namespace Overloader;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class FormattersBundle : Attribute
{
	public FormattersBundle(string bundleName, params string[] formatterNames) { }
}
