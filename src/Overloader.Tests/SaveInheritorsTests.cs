﻿namespace Overloader.Tests;

[TestFixture]
public class SaveInheritorsTests
{
	[Test]
	public void BaseTest()
	{
		const string programCs = $$"""
			using Overloader;

			namespace TestProject;

			[{{TSpecify.TagName}}(typeof(double))]
			[{{TOverload.TagName}}(typeof(float), "Test", "Test1")]
			public class Test : ITest { } 
			public interface ITest { }
			
			internal class Program
			{
				static void Main(string[] args) { } 
			}
		""";
		
		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);
		
		var assembly = result.Compilation.ToAssembly();
		var types = assembly.DefinedTypes
			.Where(type => type.Name != "Program" && type.GetInterface("ITest") is not null)
			.ToArray();
		Assert.That(types.Length, Is.EqualTo(2));
	}
}
