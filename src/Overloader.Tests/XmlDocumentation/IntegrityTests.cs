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

			[{{nameof(TSpecify)}}(typeof(double))]
			[{{nameof(TOverload)}}(typeof(float), "Program", "Program1")]
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
	}
}
