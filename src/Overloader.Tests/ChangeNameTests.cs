namespace Overloader.Tests;

[TestFixture]
public class ChangeNameTests
{
	[TestCase("ChangedName",
		"ChangedNameForType")]
	public void ChangeNameBaseTest(params string[] expectedMethodNames)
	{
		const string programCs = $$"""
			using Overloader;

			namespace TestProject;

			[{{nameof(TSpecify)}}(typeof(double))]
			[{{nameof(TOverload)}}(typeof(float), "Test", "Test1")]
			public partial class Test
			{
				[{{nameof(ChangeName)}}("ChangedName")]
				public static void TestMethod() {}
				
				[{{nameof(ChangeName)}}("ChangedNameForType", typeof(float))]
				public static void TestForTypeMethod() {}
			}
			
			internal class Program
			{
				static void Main(string[] args) { } 
			}
		""";
		
		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);
		
		var methodNames = result.Result.GeneratedTrees
			.SelectMany(tree =>
				tree.GetRoot()
					.DescendantNodes()
					.OfType<MethodDeclarationSyntax>()
					.Select(methodSyntax => methodSyntax.Identifier.Text))
			.ToHashSet();

		Assert.That(methodNames, Has.Count.EqualTo(expectedMethodNames.Length));
		foreach (string name in expectedMethodNames)
			Assert.That(methodNames, Does.Contain(name));
	}
}
