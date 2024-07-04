namespace Overloader.Tests.MultipleTemplates;

[TestFixture]
public class BaseTests
{
	[TestCase("[ typeof(float) ]")]
	[TestCase("new[] { typeof(float) }")]
	[TestCase("typeof(float)")]
	public void CheckThatParametersCountAreTheSameForOverloadAndSpecifyTest(string overloadTypes)
	{
		string programCs = 
			$$"""

			  using {{nameof(Overloader)}};

			  namespace TestProject;

			  [{{TSpecify.TagName}}([ typeof(double), typeof(int) ])]
			  [{{TOverload.TagName}}({{overloadTypes}}, "Program", "Program1")]
			  internal class Program
			  {
			  	static void Main(string[] args) { }
			  
			  	[{{ForceChanged.TagName}}()]
			  	public static string {{nameof(CheckThatParametersCountAreTheSameForOverloadAndSpecifyTest)}}() => "DEFAULT";
			  }

			  """;
		
		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Not.Empty);
		
		foreach (var diagnostic in result.GenerationDiagnostics)
		{
			Assert.That(diagnostic.Id, Is.EqualTo("OE0001"));
		}
	}
	
	[Test]
	public void CheckThatArgumentExceptionShownWhenIndexationWrongSpecifiedForHeader()
	{
		string programCs = 
			$$"""

			  using {{nameof(Overloader)}};

			  namespace TestProject;

			  [{{TSpecify.TagName}}(typeof(double))]
			  [{{TOverload.TagName}}(typeof(float), "Program", "Program1")]
			  internal class Program
			  {
			  	static void Main(string[] args) { }
			  
			  	public static string {{nameof(CheckThatArgumentExceptionShownWhenIndexationWrongSpecifiedForHeader)}}(
			  	    [{{TAttribute.TagName}}(1)] double test) =>
			  	    //$1 "EXPECTED"
			  	    "DEFAULT";
			  }

			  """;
		
		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Not.Empty);
		
		foreach (var diagnostic in result.GenerationDiagnostics)
		{
			Assert.That(diagnostic.Id, Is.EqualTo("OE0001"));
		}
	}
	
	[Test]
	public void CheckThatArgumentOutOfRangeNotShownWhenIndexationWrongSpecified()
	{
		string programCs = 
			$$"""

			  using {{nameof(Overloader)}};

			  namespace TestProject;

			  [{{TSpecify.TagName}}(typeof(double))]
			  [{{TOverload.TagName}}(typeof(float), "Program", "Program1")]
			  internal class Program
			  {
			  	static void Main(string[] args) { }
			  
			  	public static string {{nameof(CheckThatArgumentOutOfRangeNotShownWhenIndexationWrongSpecified)}}(
			  	    [{{TAttribute.TagName}}] double test) =>
			  	    //#1 "test" -> "test"
			  	    //$1 "EXPECTED"
			  	    "DEFAULT";
			  }

			  """;
		
		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);
	}
}
