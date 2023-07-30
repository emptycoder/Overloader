// ReSharper disable UnusedParameter.Local
namespace Overloader;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class IgnoreFor : Attribute
{
	public IgnoreFor(Type? type = null, string? reason = null) { }
}
