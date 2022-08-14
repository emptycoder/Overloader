using System.Reflection;

namespace Overloader.Tests.Utils;

public static class AssemblyExtensions
{
	public static TypeInfo? FindClassByName(this Assembly assembly, string name) => 
		assembly.DefinedTypes.FirstOrDefault(member => member.Name == name);
}
