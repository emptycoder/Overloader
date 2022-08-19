using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Tests.GeneratorRunner;

namespace Overloader.Tests;

public class IgnoreAllowAttributesTest
{
	[Test]
	public void BlackListModeTest()
	{
		string programCs =
			@$"
using Overloader;

namespace TestProject;

[{AttributeNames.OverloadsAttr}(typeof(float), ""Program"", ""Program1"")]
[{AttributeNames.BlackListModeAttr}]
internal class Program
{{
	static void Main(string[] args) {{ }}

	[{AttributeNames.ChangeModifierAttr}(""public"", ""private"", typeof(float))]
	[{AttributeNames.ChangeModifierAttr}(""private"", ""protected"")]
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

[{AttributeNames.OverloadsAttr}(typeof(float), ""Program"", ""Program1"")]
{(isBlackList ? $"[{AttributeNames.BlackListModeAttr}]" : string.Empty)}
internal class Program
{{
	static void Main(string[] args) {{ }}

	[{AttributeNames.ChangeModifierAttr}(""public"", ""public"")]
	[{AttributeNames.IgnoreForAttr}]
	public static void {nameof(IgnoreAllowForTest)}1() {{ }}

	[{AttributeNames.ChangeModifierAttr}(""public"", ""public"")]
	[{AttributeNames.IgnoreForAttr}(typeof(float))]
	public static void {nameof(IgnoreAllowForTest)}2() {{ }}

	[{AttributeNames.ChangeModifierAttr}(""public"", ""public"")]
	[{AttributeNames.IgnoreForAttr}(typeof(double))]
	public static void {nameof(IgnoreAllowForTest)}3() {{ }}

	[{AttributeNames.ChangeModifierAttr}(""public"", ""public"")]
	[{AttributeNames.AllowForAttr}]
	public static void {nameof(IgnoreAllowForTest)}4() {{ }}

	[{AttributeNames.ChangeModifierAttr}(""public"", ""public"")]
	[{AttributeNames.AllowForAttr}(typeof(float))]
	public static void {nameof(IgnoreAllowForTest)}5() {{ }}

	[{AttributeNames.ChangeModifierAttr}(""public"", ""public"")]
	[{AttributeNames.AllowForAttr}(typeof(double))]
	public static void {nameof(IgnoreAllowForTest)}6() {{ }}
}}
";

		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);
		Assert.That(result.Result.GeneratedTrees.Length, Is.EqualTo(1));

		var methodNames = result.Result.GeneratedTrees.SelectMany(tree =>
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
