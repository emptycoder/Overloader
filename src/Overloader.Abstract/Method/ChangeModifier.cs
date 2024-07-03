// ReSharper disable UnusedParameter.Local
namespace Overloader;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class ChangeModifier : Attribute
{
	public const string TagName = nameof(ChangeModifier);
	
	public ChangeModifier(
		string modifier,
		string newModifier,
		byte templateIndexFor = 0,
		Type? templateTypeFor = null) { }
}
