using Overloader.Tests.GeneratorRunner;

namespace Overloader.Tests;

public class StatementTests
{
	private const string SomeKindOfComment = "// SOME KIND OF USER COMMENT\n";

	[TestCase("//# \"DEFAULT\" -> \"EXPECTED\"", ExpectedResult = "EXPECTED")]
	[TestCase("//# \"DEFAULT\" -> \"EXPECTED\" : float", ExpectedResult = "EXPECTED")]
	[TestCase("//# \"DEFAULT\" -> \"EXPECTED\" : double", ExpectedResult = "DEFAULT")]
	[TestCase($"{SomeKindOfComment}//# \"DEFAULT\" -> \"EXPECTED\"", ExpectedResult = "EXPECTED")]
	[TestCase($"{SomeKindOfComment}//# \"DEFAULT\" -> \"EXPECTED\" : float", ExpectedResult = "EXPECTED")]
	[TestCase($"{SomeKindOfComment}//# \"DEFAULT\" -> \"EXPECTED\" : double", ExpectedResult = "DEFAULT")]
	public string ReplaceOperationTest(string comment)
	{
		string programCs = @$"
using {nameof(Overloader)};

namespace TestProject;

[{Attributes.OverloadAttr}(typeof(float), ""Program"", ""Program1"")]
internal class Program
{{
	static void Main(string[] args) {{ }}

	[{Attributes.ChangeModifierAttr}(""public"", ""public"")]
	public static string {nameof(ReplaceOperationTest)}()
	{{
		{comment}
		return ""DEFAULT"";
	}}
}}
";

		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);

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

	[TestCase("//$ return \"EXPECTED\";", ExpectedResult = "EXPECTED")]
	[TestCase("//$ return \"EXPECTED\"; : float", ExpectedResult = "EXPECTED")]
	[TestCase("//$ return \"EXPECTED\"; : double", ExpectedResult = "DEFAULT")]
	[TestCase($"{SomeKindOfComment}//$ return \"EXPECTED\";", ExpectedResult = "EXPECTED")]
	[TestCase($"{SomeKindOfComment}//$ return \"EXPECTED\"; : float", ExpectedResult = "EXPECTED")]
	[TestCase($"{SomeKindOfComment}//$ return \"EXPECTED\"; : double", ExpectedResult = "DEFAULT")]
	public string ChangeLineOperationTest(string comment)
	{
		string programCs = @$"
using {nameof(Overloader)};

namespace TestProject;

[{Attributes.OverloadAttr}(typeof(float), ""Program"", ""Program1"")]
internal class Program
{{
	static void Main(string[] args) {{ }}

	[{Attributes.ChangeModifierAttr}(""public"", ""public"")]
	public static string {nameof(ChangeLineOperationTest)}()
	{{
		{comment}
		return ""DEFAULT"";
	}}
}}
";

		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);

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

	[TestCase("//$ \"EXPECTED\"", ExpectedResult = "EXPECTED")]
	[TestCase("//$ \"EXPECTED\" : float", ExpectedResult = "EXPECTED")]
	[TestCase("//$ \"EXPECTED\" : double", ExpectedResult = "DEFAULT")]
	[TestCase($"{SomeKindOfComment}//$ \"EXPECTED\"", ExpectedResult = "EXPECTED")]
	[TestCase($"{SomeKindOfComment}//$ \"EXPECTED\" : float", ExpectedResult = "EXPECTED")]
	[TestCase($"{SomeKindOfComment}//$ \"EXPECTED\" : double", ExpectedResult = "DEFAULT")]
	public string ArrowTokenStatementsTest(string comment)
	{
		string programCs = @$"
using {nameof(Overloader)};

namespace TestProject;

[{Attributes.OverloadAttr}(typeof(float), ""Program"", ""Program1"")]
internal class Program
{{
	static void Main(string[] args) {{ }}

	[{Attributes.ChangeModifierAttr}(""public"", ""public"")]
	public static string {nameof(ChangeLineOperationTest)}() =>
		{comment}
		""DEFAULT"";
}}
";

		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);

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
