// ReSharper disable UnusedParameter.Local
namespace Overloader;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class SkipMode : Attribute
{
	public const string TagName = nameof(SkipMode);
	
	public SkipMode(
		bool shouldBeSkipped,
		byte templateIndexFor = 0,
		Type? templateTypeFor = null,
		string? reason = null) { }
}
