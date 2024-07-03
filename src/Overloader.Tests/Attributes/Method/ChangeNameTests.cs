namespace Overloader.Tests.Attributes.Method;

[TestFixture]
public class ChangeNameTests
{
	[TestCase("ChangedName",
		"ChangedNameForType")]
	public void BaseTest(params string[] expectedMethodNames)
	{
		const string programCs = $$"""
			using Overloader;

			namespace TestProject;

			[{{TSpecify.TagName}}(typeof(double))]
			[{{TOverload.TagName}}(typeof(float), "Test", "Test1")]
			public partial class Test
			{
				[{{ChangeName.TagName}}("ChangedName")]
				public static void TestMethod() {}
				
				[{{ChangeName.TagName}}("ChangedNameForType", templateTypeFor: typeof(float))]
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
