namespace Overloader;

[AttributeUsage(AttributeTargets.Assembly,
	AllowMultiple = true)]
public class Formatter : Attribute
{
	public const string TagName = nameof(Formatter);
	
	public Formatter(string identifier, Type type, object[] genericParams, object[] @params, params object[] transitions) { }
	public Formatter(string identifier, Type[] types, object[] genericParams, object[] @params, params object[] transitions) { }
}
