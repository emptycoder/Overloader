using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Tests.GeneratorRunner;

namespace Overloader.Tests;

public class FormatterTests
{
	// ReSharper disable once RedundantStringInterpolation
	private const string DefaultVector3Formatter = @$"
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
			}}";

	// ReSharper disable once RedundantStringInterpolation
	private const string FakeVector3Formatter = @$"
		typeof(TestProject.Vector3<>),
			new object[] {{""T""}},
			new object[]
			{{
				""X"", ""T"",
				""Y"", ""T"",
				""Z"", ""T""
			}}";

	// ReSharper disable once RedundantStringInterpolation
	private const string Vector3WithoutParams = $@"
		typeof(TestProject.Vector3<>),
			new object[] {{""T""}},
			new object[] {{ }}";

	[TestCase(DefaultVector3Formatter, null, TestName = "F: Global formatter")]
	[TestCase(null, DefaultVector3Formatter, TestName = "F: Formatter")]
	[TestCase(FakeVector3Formatter, DefaultVector3Formatter, TestName = "F: Order of formatters")]
	public void FormatterTest(string? globalFormatter, string? formatter)
	{
		string programCs =
			@$"
using Overloader;

{(globalFormatter is not null ? $"[assembly: {Constants.FormatterAttr}({globalFormatter})]" : string.Empty)}

namespace TestProject;

{(formatter is not null ? $"[{Constants.FormatterAttr}({formatter})]" : string.Empty)}
[{Constants.OverloadAttr}(typeof(float))]
internal partial class Program
{{
	static void Main(string[] args) {{ }}

	public static void {nameof(FormatterTest)}([T] Vector3<double> vec) {{ }}
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
			{"double,double,double", false},
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

	[TestCase(Vector3WithoutParams, null, TestName = "AP: Global formatter")]
	[TestCase(null, Vector3WithoutParams, TestName = "AP: Formatter")]
	public void AutoParamIntegrityTest(string? globalFormatter, string? formatter)
	{
		string programCs =
			@$"
using System;
using Overloader;

{(globalFormatter is not null ? $"[assembly: {Constants.FormatterAttr}({globalFormatter})]" : string.Empty)}

namespace TestProject;

{(formatter is not null ? $"[{Constants.FormatterAttr}({formatter})]" : string.Empty)}
[{Constants.OverloadAttr}(typeof(float))]
internal partial class Program
{{
	static void Main(string[] args) {{ }}

	public static void {nameof(AutoParamIntegrityTest)}([T] Vector3<double> vec) {{ }}
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

	[TestCase(Vector3WithoutParams, null, TestName = "DF: Global formatter")]
	[TestCase(null, Vector3WithoutParams, TestName = "DF: Formatter")]
	public void DeepFormatterUsageTest(string? globalFormatter, string? formatter)
	{
		string programCs =
			@$"
using System;
using Overloader;

{(globalFormatter is not null ? $"[assembly: {Constants.FormatterAttr}({globalFormatter})]" : string.Empty)}

namespace TestProject;

{(formatter is not null ? $"[{Constants.FormatterAttr}({formatter})]" : string.Empty)}
[{Constants.OverloadAttr}(typeof(float))]
internal partial class Program
{{
	static void Main(string[] args) {{ }}

	public static void {nameof(DeepFormatterUsageTest)}([T] Vector3<Vector3<double>> vec) {{ }}
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

namespace TestProject;

[{Constants.FormatterAttr}(typeof(TestProject.Vector3<>),
			new object[] {{""T""}},
			new object[]
			{{
				nameof(Vector3<double>.X), ""T"",
				nameof(Vector3<double>.Y), ""T"",
				nameof(Vector3<double>.Z), ""T""
			}})]
[{Constants.OverloadAttr}(typeof(float))]
internal partial class Program
{{
	static void Main(string[] args) {{ }}

	public static void {nameof(NameOfSupportTest)}([T] Vector3<Vector3<double>> vec) {{ }}
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
