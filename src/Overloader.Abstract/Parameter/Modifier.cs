// ReSharper disable UnusedParameter.Local
namespace Overloader;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
public class Modifier : Attribute
{
	public const string TagName = nameof(Modifier);
	
	public Modifier(string modifier, string? insteadOf = null, Type? formatterType = null) { }
}
