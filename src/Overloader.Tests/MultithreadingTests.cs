﻿namespace Overloader.Tests;

[TestFixture]
public class MultithreadingTests
{
	[Test]
	public void BaseTest()
	{
		const string programCs = @$"
using Overloader;

[assembly: {Formatter.TagName}(
			""Vector3"",
			typeof(TestProject.Vector3<>),
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

[{TSpecify.TagName}(new[] {{ typeof(double) }}, ""Vector3"")]
[{TOverload.TagName}(new[] {{ typeof(float) }})]
internal partial class Program
{{
	static void Main(string[] args) {{ }}

	public static void TestMethod1([{Integrity.TagName}][{TAttribute.TagName}] Vector3<double> vec, Vector3<double> vec1) {{ }}

	[return: {TAttribute.TagName}]
	public static double TestMethod2([{TAttribute.TagName}] Vector3<double> vec, [{TAttribute.TagName}] Vector3<double> vec1)
	{{
		Test(vec);
		//# ""double"" -> ""${{T}}""
		return (double) (vec.X + vec1.X + vec.Y + vec1.Y + vec.Z + vec1.Z);
	}}

	private static void Test(Vector3<double> vec123) {{}}
	private static void Test(Vector3<float> vec123) {{}}
	private static void Test(double x, double y, double z) {{}}
	private static void Test(float x, float y, float z) {{}}

	public static void TestMethod3(Vector3<double> vec, [{TAttribute.TagName}] double vec1) {{ }}
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

[{TSpecify.TagName}(new[] {{ typeof(double) }}, ""Vector3"")]
[{TOverload.TagName}(new[] {{ typeof(float) }})]
internal partial class TestClass{index}
{{
	public static void TestMethod1([{Integrity.TagName}][{TAttribute.TagName}] Vector3<double> vec, Vector3<double> vec1) {{ }}

	[return: {TAttribute.TagName}]
	public static double TestMethod2([{TAttribute.TagName}] Vector3<double> vec, [{TAttribute.TagName}] Vector3<double> vec1)
	{{
		//# ""double"" -> ""${{T}}""
		return (double) (vec.X + vec1.X + vec.Y + vec1.Y + vec.Z + vec1.Z);
	}}

	public static void TestMethod3(Vector3<double> vec, [{TAttribute.TagName}] double vec1) {{ }}
}}
";

		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(sources);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);
	}
}
