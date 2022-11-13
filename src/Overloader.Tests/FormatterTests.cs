namespace Overloader.Tests;

public class FormatterTests
{
	// ReSharper disable once RedundantStringInterpolation
	private const string DefaultVector3Formatter = @$"
		""Vector3"",
		new[] {{ typeof(TestProject.Vector3<>) }},
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
			}}";

	// ReSharper disable once RedundantStringInterpolation
	private const string Vector3WithoutParams = $@"
		""Vector3"",
		typeof(TestProject.Vector3<>),
			new object[] {{""T""}},
			new object[] {{ }}";

	[Test]
	public void FormatterTest()
	{
		const string programCs = @$"
using Overloader;

[assembly: {Constants.FormatterAttr}({DefaultVector3Formatter})]

namespace TestProject;

[{Constants.TSpecifyAttr}(typeof(double), ""Vector3"")]
[{Constants.OverloadAttr}(typeof(float))]
internal partial class Program
{{
	static void Main(string[] args) {{ }}

	public static void {nameof(FormatterTest)}([{Constants.TAttr}] Vector3<double> vec) {{ }}
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

		var methodOverloads = new Dictionary<string, bool>(3)
		{
			{"double,double,long", false},
			{"float,double,double", false},
			{"TestProject.Vector3<float>", false}
		};

		foreach (string? identifier in from generatedTree in result.Result.GeneratedTrees
		         where !Path.GetFileName(generatedTree.FilePath).Equals($"{Constants.AttributesFileNameWoExt}.g.cs")
		         select generatedTree.GetRoot()
			         .DescendantNodes()
			         .OfType<MethodDeclarationSyntax>()
		         into methods
		         from method in methods
		         select string.Join(',', method.ParameterList.Parameters.Select(parameter => parameter.Type!.ToString()))
		         into identifier
		         where methodOverloads.ContainsKey(identifier)
		         select identifier)
			methodOverloads[identifier] = true;

		foreach (var kv in methodOverloads)
			Assert.That(kv.Value, Is.True);
	}

	[Test]
	public void AutoParamIntegrityTest()
	{
		const string programCs = @$"
using System;
using Overloader;

[assembly: {Constants.FormatterAttr}({Vector3WithoutParams})]

namespace TestProject;

[{Constants.TSpecifyAttr}(typeof(double), ""Vector3"")]
[{Constants.OverloadAttr}(typeof(float))]
internal partial class Program
{{
	static void Main(string[] args) {{ }}

	public static void {nameof(AutoParamIntegrityTest)}([{Constants.TAttr}] Vector3<double> vec) {{ }}
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

		int countOfMethods = result.Result.GeneratedTrees.Where(generatedTree =>
				!Path.GetFileName(generatedTree.FilePath).Equals($"{Constants.AttributesFileNameWoExt}.g.cs"))
			.SelectMany(generatedTree => generatedTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
			.Sum(method => Convert.ToByte(method.Identifier.ToString().Equals(nameof(AutoParamIntegrityTest))));
		Assert.That(countOfMethods, Is.EqualTo(1));
	}

	[Test]
	public void DeepFormatterUsageTest()
	{
		const string programCs = @$"
using System;
using Overloader;

[assembly: {Constants.FormatterAttr}({Vector3WithoutParams})]

namespace TestProject;

[{Constants.TSpecifyAttr}(typeof(double), ""Vector3"")]
[{Constants.OverloadAttr}(typeof(float))]
internal partial class Program
{{
	static void Main(string[] args) {{ }}

	public static void {nameof(DeepFormatterUsageTest)}([{Constants.TAttr}] Vector3<Vector3<double>> vec) {{ }}
	// For Overload conflict
	public static void {nameof(DeepFormatterUsageTest)}(Vector3<float> vec) {{ }}
	public static void {nameof(DeepFormatterUsageTest)}(float vec) {{ }}
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

	[Test]
	public void NameOfSupportTest()
	{
		const string programCs =
			@$"
using System;
using Overloader;

[assembly: {Constants.FormatterAttr}(
			""Vector3"",
			typeof(TestProject.Vector3<>),
			new object[] {{""T""}},
			new object[]
			{{
				nameof(TestProject.Vector3<double>.X), ""T"",
				nameof(TestProject.Vector3<double>.Y), ""T"",
				nameof(TestProject.Vector3<double>.Z), ""T""
			}})]

namespace TestProject;

[{Constants.TSpecifyAttr}(typeof(double), ""Vector3"")]
[{Constants.OverloadAttr}(typeof(float))]
internal partial class Program
{{
	static void Main(string[] args) {{ }}

	public static void {nameof(NameOfSupportTest)}([{Constants.TAttr}] Vector3<Vector3<double>> vec) {{ }}
	// For Overload conflict
	public static void {nameof(NameOfSupportTest)}(Vector3<float> vec) {{ }}
	public static void {nameof(NameOfSupportTest)}(float vec) {{ }}
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
