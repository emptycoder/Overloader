﻿namespace Overloader.Tests;

public class ChangeNameTests
{
	[TestCase("ChangedName",
		"ChangedNameForType")]
	public void ChangeNameBaseTest(params string[] expectedMethodNames)
	{
		const string programCs = $$"""
			using Overloader;

			namespace TestProject;

			[{{Constants.TSpecifyAttr}}(typeof(double))]
			[{{Constants.OverloadAttr}}(typeof(float), "Test", "Test1")]
			public partial class Test
			{
				[{{Constants.ChangeNameAttr}}("ChangedName")]
				public static void TestMethod() {}
				
				[{{Constants.ChangeNameAttr}}("ChangedNameForType", typeof(float))]
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
			.Where(tree => !Path.GetFileName(tree.FilePath).Equals($"{Constants.AttributesFileNameWoExt}.g.cs"))
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
