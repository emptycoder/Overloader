using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Tests.GeneratorRunner;

namespace Overloader.Tests;

public class ChangeModifierTests
{
	[Test]
	public void ModifierTest()
	{
		const string programCs = @$"
using Overloader;

namespace TestProject;

[{Attributes.OverloadAttr}(typeof(float), ""Program"", ""Program1"")]
internal class Program
{{
	static void Main(string[] args) {{ }}

	[{Attributes.ChangeModifierAttr}(""public"", ""private"", typeof(float))]
	[{Attributes.ChangeModifierAttr}(""public"", ""internal"", typeof(double))]
	[{Attributes.ChangeModifierAttr}(""private"", ""protected"")]
	public static void {nameof(ModifierTest)}() {{ }}
}}
";

		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);

		var modifierOverloads = new Dictionary<string, bool>(3)
		{
			{"protected,static", false}
		};

		foreach (string? identifier in from generatedTree in result.Result.GeneratedTrees
		         where !Path.GetFileName(generatedTree.FilePath).Equals($"{nameof(Attributes)}.g.cs")
		         select generatedTree.GetRoot()
			         .DescendantNodes()
			         .OfType<MethodDeclarationSyntax>()
		         into methods
		         from method in methods
		         select string.Join(',', method.Modifiers.Select(modifier => modifier.ToString()))
		         into identifier
		         where modifierOverloads.ContainsKey(identifier)
		         select identifier)
			modifierOverloads[identifier] = true;

		foreach (var kv in modifierOverloads)
			Assert.That(kv.Value, Is.True);
	}
}
