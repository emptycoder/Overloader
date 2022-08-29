using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Tests.GeneratorRunner;

namespace Overloader.Tests;

public class IgnoreAllowAttributeTests
{
	[Test]
	public void BlackListModeTest()
	{
		const string programCs = @$"
using Overloader;

namespace TestProject;

[{Attributes.OverloadAttr}(typeof(float), ""Program"", ""Program1"")]
[{Attributes.BlackListModeAttr}]
internal class Program
{{
	static void Main(string[] args) {{ }}

	[{Attributes.ChangeModifierAttr}(""public"", ""private"", typeof(float))]
	[{Attributes.ChangeModifierAttr}(""private"", ""protected"")]
	public static void {nameof(BlackListModeTest)}() {{ }}
}}
";

		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);
		Assert.That(result.Result.GeneratedTrees.Sum(tree =>
			tree.GetRoot()
				.DescendantNodes()
				.OfType<MethodDeclarationSyntax>()
				.Count()), Is.EqualTo(0));
	}

	[TestCase(true, $"{nameof(IgnoreAllowForTest)}4", $"{nameof(IgnoreAllowForTest)}5")]
	[TestCase(false, $"{nameof(IgnoreAllowForTest)}3", $"{nameof(IgnoreAllowForTest)}4",
		$"{nameof(IgnoreAllowForTest)}5", $"{nameof(IgnoreAllowForTest)}6")]
	public void IgnoreAllowForTest(bool isBlackList, params string[] expectedMethodNames)
	{
		string programCs =
			@$"
using Overloader;

namespace TestProject;

[{Attributes.OverloadAttr}(typeof(float), ""Program"", ""Program1"")]
{(isBlackList ? $"[{Attributes.BlackListModeAttr}]" : string.Empty)}
internal class Program
{{
	static void Main(string[] args) {{ }}

	[{Attributes.ChangeModifierAttr}(""public"", ""public"")]
	[{Attributes.IgnoreForAttr}]
	public static void {nameof(IgnoreAllowForTest)}1() {{ }}

	[{Attributes.ChangeModifierAttr}(""public"", ""public"")]
	[{Attributes.IgnoreForAttr}(typeof(float))]
	public static void {nameof(IgnoreAllowForTest)}2() {{ }}

	[{Attributes.ChangeModifierAttr}(""public"", ""public"")]
	[{Attributes.IgnoreForAttr}(typeof(double))]
	public static void {nameof(IgnoreAllowForTest)}3() {{ }}

	[{Attributes.ChangeModifierAttr}(""public"", ""public"")]
	[{Attributes.AllowForAttr}]
	public static void {nameof(IgnoreAllowForTest)}4() {{ }}

	[{Attributes.ChangeModifierAttr}(""public"", ""public"")]
	[{Attributes.AllowForAttr}(typeof(float))]
	public static void {nameof(IgnoreAllowForTest)}5() {{ }}

	[{Attributes.ChangeModifierAttr}(""public"", ""public"")]
	[{Attributes.AllowForAttr}(typeof(double))]
	public static void {nameof(IgnoreAllowForTest)}6() {{ }}
}}
";

		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);

		var methodNames = result.Result.GeneratedTrees
			.Where(tree => !Path.GetFileName(tree.FilePath).Equals($"{nameof(Attributes)}.g.cs"))
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
