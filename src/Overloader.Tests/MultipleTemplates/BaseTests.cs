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
			  
			  	[{{ChangeModifier.TagName}}("public", "public")]
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
}
