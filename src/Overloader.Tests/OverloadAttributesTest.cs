using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Tests.GeneratorRunner;

namespace Overloader.Tests;

public class OverloadAttributesTest
{
	[TestCase("public", "public", "static")]
	[TestCase("internal", "internal", "static")]
	public void NewClassOverloadTest(string accessModifier, params string[] expectedModifiers)
	{
		const string className = "Vector2DExtension";
		const string regex = "2D";
		const string regexReplacement = "2F";
		string programCs =
			@$"
using Overloader;

namespace TestProject;

internal class Program
{{
	static void Main(string[] args) {{ }}
}}

[{AttributeNames.OverloadAttr}(typeof(float), ""{regex}"", ""{regexReplacement}"")]
{accessModifier} static class {className} {{ }}
";
		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);
		Assert.That(result.Result.GeneratedTrees, Has.Length.EqualTo(1));

		string newClassName = Regex.Replace(className, regex, regexReplacement);
		var classes = result.Result.GeneratedTrees[0].GetRoot()
			.DescendantNodes()
			.OfType<ClassDeclarationSyntax>()
			.ToArray();
		Assert.That(classes, Has.Length.EqualTo(1));

		var @class = classes.First();
		Assert.That(@class.Identifier.Text, Is.EqualTo(newClassName));

		for (int index = 0; index < expectedModifiers.Length; index++)
			Assert.That(expectedModifiers[index], Is.EqualTo(@class.Modifiers[index].Text));
	}
}
