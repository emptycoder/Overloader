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

	[Test]
	public void ArrowTokenSingleLineProblemTest()
	{
		const string programCs = @$"
using {nameof(Overloader)};

namespace TestProject;

[{Attributes.OverloadAttr}(typeof(float), ""Program"", ""Program1"")]
internal class Program
{{
	static void Main(string[] args) {{ }}

	[{Attributes.ChangeModifierAttr}(""public"", ""public"")]
	public static string {nameof(ArrowTokenSingleLineProblemTest)}() => ""DEFAULT"";
}}
";

		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);

		var assembly = result.Compilation.ToAssembly();
		var method = assembly.DefinedTypes
			.Where(type => type.Name != "Program")
			.SelectMany(type => type.DeclaredMethods)
			.Single(method => method.Name.Contains(nameof(ArrowTokenSingleLineProblemTest)));

		Assert.That(method, Is.Not.Null);
		object? resultObj = method.Invoke(null, null);
		Assert.That(resultObj, Is.Not.Null);
		Assert.That(resultObj is string, Is.True);
		Assert.That((string) resultObj!, Is.EqualTo("DEFAULT"));
	}

	[Test]
	public void MethodParamsOverloadTest()
	{
		const string programCs = @$"
using {nameof(Overloader)};

namespace TestProject;

[{Attributes.OverloadAttr}(typeof(float), ""Program"", ""Program1"")]
internal class Program
{{
	static void Main(string[] args) {{ }}
}}

[{Attributes.FormatterAttr}(typeof(TestProject.Vector3<>),
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
[Overload(typeof(float), ""3D"", ""3F"")]
public static class Vec3DExt
{{
	[return: T]
	public static double AngleCos([Integrity][T] this ref Vector3<double> current, [T] in Vector3<double> vector)
	{{
		// TEST
		return 0;
	}}

	[return: T]
	public static double Angle([Integrity][T] this ref Vector3<double> current, [T] in Vector3<double> vector)
	{{
		return AngleCos(ref current, in vector);
	}}
}}

public struct Vector3<T>
{{
	public double X;
	public T Y {{ get; set; }}
	internal T Z {{ get; private set; }}
}}
";

		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);

		var assembly = result.Compilation.ToAssembly();
		int methodsCount = assembly.DefinedTypes
			.Where(type => type.Name != "Program")
			.SelectMany(type => type.DeclaredMethods)
			.Sum(method => Convert.ToSByte(method.Name.Equals("Angle")));
		Assert.That(methodsCount, Is.EqualTo(3));
	}
}
