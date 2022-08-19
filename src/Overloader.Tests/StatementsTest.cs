using Overloader.Tests.GeneratorRunner;

namespace Overloader.Tests;

public class StatementsTest
{
	[TestCase("\"DEFAULT\" -> \"EXPECTED\"", ExpectedResult = "EXPECTED")]
	[TestCase("\"DEFAULT\" -> \"EXPECTED\" : float", ExpectedResult = "EXPECTED")]
	[TestCase("\"DEFAULT\" -> \"EXPECTED\" : double", ExpectedResult = "DEFAULT")]
	public string ReplaceOperationTest(string comment)
	{
		string programCs = @$"
using Overloader;

namespace TestProject;

[{AttributeNames.OverloadsAttr}(typeof(float), ""Program"", ""Program1"")]
internal class Program
{{
	static void Main(string[] args) {{ }}

	[{AttributeNames.ChangeModifierAttr}(""public"", ""public"")]
	public static string {nameof(ReplaceOperationTest)}()
	{{
		//# {comment}
		return ""DEFAULT"";
	}}
}}
";

		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);
		Assert.That(result.Result.GeneratedTrees, Has.Length.EqualTo(1));

		var assembly = result.Compilation.ToAssembly();
		var method = assembly.DefinedTypes
			.Where(type => type.Name != "Program")
			.SelectMany(type => type.DeclaredMethods)
			.Single(method => method.Name.Contains(nameof(ReplaceOperationTest)));
		Assert.That(method, Is.Not.Null);
		object? resultObj = method.Invoke(null, null);

		Assert.That(resultObj, Is.Not.Null);
		Assert.That(resultObj is string, Is.True);
		return (string) resultObj!;
	}

	[TestCase("return \"EXPECTED\";", ExpectedResult = "EXPECTED")]
	[TestCase("return \"EXPECTED\"; : float", ExpectedResult = "EXPECTED")]
	[TestCase("return \"EXPECTED\"; : double", ExpectedResult = "DEFAULT")]
	public string ChangeLineOperationTest(string comment)
	{
		string programCs = @$"
using Overloader;

namespace TestProject;

[{AttributeNames.OverloadsAttr}(typeof(float), ""Program"", ""Program1"")]
internal class Program
{{
	static void Main(string[] args) {{ }}

	[{AttributeNames.ChangeModifierAttr}(""public"", ""public"")]
	public static string {nameof(ChangeLineOperationTest)}()
	{{
		//$ {comment}
		return ""DEFAULT"";
	}}
}}
";

		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);
		Assert.That(result.Result.GeneratedTrees, Has.Length.EqualTo(1));

		var assembly = result.Compilation.ToAssembly();
		var method = assembly.DefinedTypes
			.Where(type => type.Name != "Program")
			.SelectMany(type => type.DeclaredMethods)
			.Single(method => method.Name.Contains(nameof(ChangeLineOperationTest)));
		Assert.That(method, Is.Not.Null);
		object? resultObj = method.Invoke(null, null);

		Assert.That(resultObj, Is.Not.Null);
		Assert.That(resultObj is string, Is.True);
		return (string) resultObj!;
	}
}
