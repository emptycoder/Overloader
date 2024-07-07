// ReSharper disable UnusedParameter.Local
namespace Overloader;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = true)]
public class Modifier : Attribute
{
	public const string TagName = nameof(Modifier);
	
	public Modifier(
		string modifier,
		string? insteadOf = null,
		byte templateIndexFor = 0,
		Type? templateTypeFor = null) { }
}
