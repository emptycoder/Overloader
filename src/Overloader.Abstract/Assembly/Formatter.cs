// ReSharper disable UnusedParameter.Local
namespace Overloader;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class Formatter : Attribute
{
	public const string TagName = nameof(Formatter);
	
	public Formatter(string formatterName, Type type, object[] genericParams, object[] @params, params object[] transitions) { }
	public Formatter(string formatterName, Type[] types, object[] genericParams, object[] @params, params object[] transitions) { }
}
