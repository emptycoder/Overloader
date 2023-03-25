namespace Overloader;

[AttributeUsage(AttributeTargets.Assembly,
	AllowMultiple = true)]
public class FormatterAttribute : Attribute
{
	public FormatterAttribute(string identifier, Type type, object[] genericParams, object[] @params, params object[] transitions) { }
	public FormatterAttribute(string identifier, Type[] types, object[] genericParams, object[] @params, params object[] transitions) { }
}
