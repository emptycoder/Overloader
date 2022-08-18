using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Tests.GeneratorRunner;

namespace Overloader.Tests;

public class FormattersTest
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

	[TestCase(DefaultVector3Formatter, null, TestName = "Global formatter")]
	[TestCase(null, DefaultVector3Formatter, TestName = "Formatter")]
	[TestCase(FakeVector3Formatter, DefaultVector3Formatter, TestName = "Order of formatters")]
	public void FormatterTest(string? globalFormatter, string? formatter)
	{
		string programCs =
			@$"
using Overloader;

{(globalFormatter is not null ? $"[assembly: {AttributeNames.FormatterAttr}({globalFormatter})]" : string.Empty)}

namespace TestProject;

{(formatter is not null ? $"[{AttributeNames.FormatterAttr}({formatter})]" : string.Empty)}
[{AttributeNames.OverloadsAttr}(typeof(float))]
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

		var generatedTrees = result.Result.GeneratedTrees;
		Assert.That(generatedTrees, Has.Length.EqualTo(2));

		var methodOverloads = new Dictionary<string, bool>(3)
		{
			{"double,double,double", false},
			{"float,double,double", false},
			{"TestProject.Vector3<float>", false}
		};

		foreach (string? identifier in from generatedTree in generatedTrees
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
}
