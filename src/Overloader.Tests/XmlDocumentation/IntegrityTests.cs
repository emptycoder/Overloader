using Overloader.Tests.Utils;

namespace Overloader.Tests.XmlDocumentation;

[TestFixture]
public class IntegrityTests
{
	[Test]
	public void BaseTest()
	{
		const string programCs = $$"""
			using Overloader;

			namespace TestProject;

			[{{TSpecify.TagName}}(typeof(double))]
			[{{TOverload.TagName}}(typeof(float), "Program", "Program1")]
			internal partial class Program
			{
				static void Main(string[] args) { }
		                           
				/// <summary>
		        /// Summary
		        /// </summary>
		        /// <param name="val">description of param 0</param>
		        /// <param name="val1">description of param 1</param>
		        /// <returns>test</returns>
		        public static string {{nameof(BaseTest)}}(
					[{{TAttribute.TagName}}] double val,
					[{{TAttribute.TagName}}] double val1) => string.Empty;
		     }
		""";
		
		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);

		string expectedTrivia = """
		    // Generated by: IntegrityOverload
		    /// <summary>
		    /// Summary
		    /// </summary>
		    /// <param name="val">description of param 0</param>
		    /// <param name="val1">description of param 1</param>
		    /// <returns>test</returns>
		    """.Minify();

		foreach (string trivia in from generatedTree in result.Result.GeneratedTrees
		         select generatedTree.GetRoot()
			         .DescendantNodes()
			         .OfType<MethodDeclarationSyntax>()
		         into methods
		         from method in methods
		         select method.GetLeadingTrivia().ToFullString()
		         into trivia
		         select trivia)
		{
			Assert.That(trivia.Minify(), Is.EqualTo(expectedTrivia));
		}
	}
}
