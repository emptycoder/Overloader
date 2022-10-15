using Overloader.Tests.GeneratorRunner;

namespace Overloader.Tests;

public class CombineWithTests
{
	[Test]
	public void CombineWithBaseTest()
	{
		const string programCs =
			@$"
using System;
using Overloader;

namespace TestProject;

[{Constants.FormatterAttr}(typeof(TestProject.Vector3<>),
			new object[] {{""T""}},
			new object[]
			{{
				""X"", ""T"",
				""Y"", ""T"",
				""Z"", ""T""
			}})]
[{Constants.OverloadAttr}(typeof(float))]
internal partial class Program
{{
	static void Main(string[] args) {{ }}

	public static void {nameof(CombineWithBaseTest)}([T] ref Vector3<double> vec, [T][CombineWith(""vec"")] Vector3<double> vec1) {{ }}
}}

internal struct Vector3<T>
{{
	public double X;
	public T Y {{ get; set; }}
	internal T Z {{ get; private set; }}
}}
";

		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);
	}
}
