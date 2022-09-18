﻿using Overloader.Tests.GeneratorRunner;

namespace Overloader.Tests;

public class MultithreadingTests
{
	[Test]
	public void ManyCodeGeneration()
	{
		const string programCs = @$"
using Overloader;

[assembly: {Constants.FormatterAttr}(typeof(TestProject.Vector3<>),
			new object[] {{""T""}},
			new object[]
			{{
				""X"", ""T"",
				""Y"", typeof(double),
				""Z"", new[]
				{{
					typeof(float), typeof(double),
					typeof(double), typeof(long)
				}}
			}})]

namespace TestProject;

[{Constants.OverloadAttr}(typeof(float))]
internal partial class Program
{{
	static void Main(string[] args) {{ }}

	public static void TestMethod1([Integrity][T] Vector3<double> vec, Vector3<double> vec1) {{ }}

	[return: T]
	public static double TestMethod2([T] Vector3<double> vec, [T] Vector3<double> vec1)
	{{
		Test(vec);
		//# ""double"" -> ""${{T}}""
		return (double) (vec.X + vec1.X + vec.Y + vec1.Y + vec.Z + vec1.Z);
	}}

	private static void Test(Vector3<double> vec123) {{}}
	private static void Test(Vector3<float> vec123) {{}}
	private static void Test(double x, double y, double z) {{}}
	private static void Test(float x, float y, float z) {{}}

	public static void TestMethod3(Vector3<double> vec, [T] double vec1) {{ }}
}}

internal struct Vector3<T>
{{
	public double X;
	public T Y {{ get; set; }}
	internal T Z {{ get; private set; }}
}}
";

		const uint countOfSourceFiles = 100;
		string[] sources = new string[countOfSourceFiles];
		sources[0] = programCs;
		for (uint index = 1; index < countOfSourceFiles; index++)
			sources[index] = $@"
using Overloader;

namespace TestProject;

[{Constants.OverloadAttr}(typeof(float))]
internal partial class TestClass{index}
{{
	public static void TestMethod1([Integrity][T] Vector3<double> vec, Vector3<double> vec1) {{ }}

	[return: T]
	public static double TestMethod2([T] Vector3<double> vec, [T] Vector3<double> vec1)
	{{
		//# ""double"" -> ""${{T}}""
		return (double) (vec.X + vec1.X + vec.Y + vec1.Y + vec.Z + vec1.Z);
	}}

	public static void TestMethod3(Vector3<double> vec, [T] double vec1) {{ }}
}}
";

		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(sources);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);
	}
}
