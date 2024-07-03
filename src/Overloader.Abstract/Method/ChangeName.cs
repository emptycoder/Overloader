// ReSharper disable UnusedParameter.Local
namespace Overloader;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class ChangeName : Attribute
{
	public ChangeName(
		string newName,
		byte templateIndexFor = 0,
		Type? templateTypeFor = null) { }
}
