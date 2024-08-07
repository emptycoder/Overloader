﻿namespace Overloader.Tests.Attributes.Method;

[TestFixture]
public class ModifierTests
{
	[Test]
	public void BaseTest()
	{
		const string programCs = 
			$$"""

			  using Overloader;

			  namespace TestProject;

			  [{{TSpecify.TagName}}(typeof(double))]
			  [{{TOverload.TagName}}(typeof(float), "Program", "Program1")]
			  internal class Program
			  {
			  	static void Main(string[] args) { }
			  
			  	[{{Modifier.TagName}}("private", "public", templateTypeFor: typeof(float))]
			  	[{{Modifier.TagName}}("internal", "public", templateTypeFor: typeof(double))]
			  	[{{Modifier.TagName}}("protected", "private")]
			  	public static void {{nameof(BaseTest)}}() { }
			  }

			  """;

		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);

		var modifierOverloads = new Dictionary<string, bool>(3)
		{
			{"protected,static", false}
		};

		foreach (string? identifier in from generatedTree in result.Result.GeneratedTrees
		         select generatedTree.GetRoot()
			         .DescendantNodes()
			         .OfType<MethodDeclarationSyntax>()
		         into methods
		         from method in methods
		         select string.Join(',', method.Modifiers.Select(modifier => modifier.ToString()))
		         into identifier
		         select identifier)
		{
			Assert.That(modifierOverloads, Does.ContainKey(identifier));
			modifierOverloads[identifier] = true;
		}

		Assert.That(modifierOverloads, Does.Not.ContainValue(false));
	}
}
