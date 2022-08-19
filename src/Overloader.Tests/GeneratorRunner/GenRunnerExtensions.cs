using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Overloader.Tests.GeneratorRunner;

public static class GenRunnerExtensions
{
	public static Assembly ToAssembly(this Compilation compilation)
	{
		using var ms = new MemoryStream();
		compilation.Emit(ms);
		return Assembly.Load(ms.ToArray());
	}
}
