namespace Overloader.Tests.MultipleTemplates;

[TestFixture]
public class StatementTests
{
	private const string SomeKindOfComment = "// SOME KIND OF USER COMMENT\n";
	
	[TestCase("//#1 \"DEFAULT\" -> \"EXPECTED\"", ExpectedResult = "EXPECTED")]
	[TestCase("//#1 \"DEFAULT\" -> \"EXPECTED\" : float", ExpectedResult = "DEFAULT")]
	[TestCase("//#1 \"DEFAULT\" -> \"EXPECTED\" : short", ExpectedResult = "EXPECTED")]
	[TestCase($"{SomeKindOfComment}//#1 \"DEFAULT\" -> \"EXPECTED\"", ExpectedResult = "EXPECTED")]
	[TestCase($"{SomeKindOfComment}//#1 \"DEFAULT\" -> \"EXPECTED\" : float", ExpectedResult = "DEFAULT")]
	[TestCase($"{SomeKindOfComment}//#1 \"DEFAULT\" -> \"EXPECTED\" : short", ExpectedResult = "EXPECTED")]
	public string ReplaceOperationTest(string comment)
	{
		string programCs = 
			$$"""

			  using {{nameof(Overloader)}};

			  namespace TestProject;

			  [{{TSpecify.TagName}}(new[] { typeof(double), typeof(int) })]
			  [{{TOverload.TagName}}(new[] { typeof(float), typeof(short) }, "Program", "Program1")]
			  internal class Program
			  {
			  	static void Main(string[] args) { }
			  
			  	[{{ChangeModifier.TagName}}("public", "public")]
			  	public static string {{nameof(ReplaceOperationTest)}}()
			  	{
			  		{{comment}}
			  		return "DEFAULT";
			  	}
			  }

			  """;

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

	[TestCase("//#1 \"${T}\" -> \"EXPECTED\"", ExpectedResult = "EXPECTED")]
	[TestCase("//#1 \"${T}\" -> \"\"", ExpectedResult = "")]
	public string ReplaceTemplateOnKeyOperationTest(string comment)
	{
		string programCs = 
			$$"""

			  using {{nameof(Overloader)}};

			  namespace TestProject;

			  [{{TSpecify.TagName}}(new[] { typeof(double), typeof(double) })]
			  [{{TOverload.TagName}}(new[] { typeof(float), typeof(float) }, "Program", "Program1")]
			  internal class Program
			  {
			  	static void Main(string[] args) { }
			  
			  	[{{ChangeModifier.TagName}}("public", "public")]
			  	public static string {{nameof(ReplaceOperationTest)}}()
			  	{
			  		{{comment}}
			  		return "float";
			  	}
			  }

			  """;
		
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

	[TestCase("//$1 return \"EXPECTED\";", ExpectedResult = "EXPECTED")]
	[TestCase("//$1 return \"EXPECTED\"; : short", ExpectedResult = "EXPECTED")]
	[TestCase("//$1 return \"EXPECTED\"; : double", ExpectedResult = "DEFAULT")]
	[TestCase($"{SomeKindOfComment}//$1 return \"EXPECTED\";", ExpectedResult = "EXPECTED")]
	[TestCase($"{SomeKindOfComment}//$1 return \"EXPECTED\"; : double, short", ExpectedResult = "EXPECTED")]
	[TestCase($"{SomeKindOfComment}//$1 return \"EXPECTED\"; : short, double", ExpectedResult = "EXPECTED")]
	[TestCase($"{SomeKindOfComment}//$1 return \"EXPECTED\"; : short", ExpectedResult = "EXPECTED")]
	[TestCase($"{SomeKindOfComment}//$1 return \"EXPECTED\"; : int", ExpectedResult = "DEFAULT")]
	[TestCase($"{SomeKindOfComment}//$1 return \"EXPECTED\"; : int,,,", ExpectedResult = "DEFAULT")]
	public string ChangeLineOperationTest(string comment)
	{
		string programCs = 
			$$"""

			  using {{nameof(Overloader)}};

			  namespace TestProject;

			  [{{TSpecify.TagName}}(new[] { typeof(double), typeof(int) })]
			  [{{TOverload.TagName}}(new[] { typeof(float), typeof(short) }, "Program", "Program1")]
			  internal class Program
			  {
			  	static void Main(string[] args) { }
			  
			  	[{{ChangeModifier.TagName}}("public", "public")]
			  	public static string {{nameof(ChangeLineOperationTest)}}()
			  	{
			  		{{comment}}
			  		return "DEFAULT";
			  	}
			  }

			  """;

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

	[TestCase("//$1 \"EXPECTED\"", ExpectedResult = "EXPECTED")]
	[TestCase("//$1 \"EXPECTED\" : short", ExpectedResult = "EXPECTED")]
	[TestCase("//$1 \"EXPECTED\" : double", ExpectedResult = "DEFAULT")]
	[TestCase($"{SomeKindOfComment}//$1 \"EXPECTED\"", ExpectedResult = "EXPECTED")]
	[TestCase($"{SomeKindOfComment}//$1 \"EXPECTED\" : short", ExpectedResult = "EXPECTED")]
	[TestCase($"{SomeKindOfComment}//$1 \"EXPECTED\" : double", ExpectedResult = "DEFAULT")]
	public string ArrowTokenStatementsTest(string comment)
	{
		string programCs = 
			$$"""

			  using {{nameof(Overloader)}};

			  namespace TestProject;

			  [{{TSpecify.TagName}}(new[] { typeof(double), typeof(int) })]
			  [{{TOverload.TagName}}(new[] { typeof(float), typeof(short) }, "Program", "Program1")]
			  internal class Program
			  {
			  	static void Main(string[] args) { }
			  
			  	[{{ChangeModifier.TagName}}("public", "public")]
			  	public static string {{nameof(ChangeLineOperationTest)}}() =>
			  		{{comment}}
			  		"DEFAULT";
			  }

			  """;

		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);

		var assembly = result.Compilation.ToAssembly();
		var method = assembly.DefinedTypes
			.Where(type => type.Name != "Program")
			.SelectMany(type => type.DeclaredMethods)
			.Single(method => method.Name.Contains(nameof(ChangeLineOperationTest)));
		Assert.That(method, Is.Not.Null);
		object? resultObj = method. Invoke(null, null);

		Assert.That(resultObj, Is.Not.Null);
		Assert.That(resultObj is string, Is.True);
		return (string) resultObj!;
	}

	[Test]
	public void ArrowTokenSingleLineProblemTest()
	{
		const string programCs = 
			$$"""

			  using {{nameof(Overloader)}};

			  namespace TestProject;

			  [{{TSpecify.TagName}}(new[] { typeof(double), typeof(int) })]
			  [{{TOverload.TagName}}(new[] { typeof(float), typeof(short) }, "Program", "Program1")]
			  internal class Program
			  {
			  	static void Main(string[] args) { }
			  
			  	[{{ChangeModifier.TagName}}("public", "public")]
			  	public static string {{nameof(ArrowTokenSingleLineProblemTest)}}() => "DEFAULT";
			  }

			  """;

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
}
