namespace Overloader.Tests;

public class IgnoreAllowAttributeTests
{
	[Test]
	public void BlackListModeTest()
	{
		const string programCs = @$"
using Overloader;

namespace TestProject;

[{Constants.TSpecifyAttr}(typeof(double))]
[{Constants.TOverloadAttr}(typeof(float), ""Program"", ""Program1"")]
[{Constants.BlackListModeAttr}]
internal class Program
{{
	static void Main(string[] args) {{ }}

	[{Constants.ChangeModifierAttr}(""public"", ""private"", typeof(float))]
	[{Constants.ChangeModifierAttr}(""private"", ""protected"")]
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

	[TestCase(true,
		$"{nameof(IgnoreAllowForTest)}4",
		$"{nameof(IgnoreAllowForTest)}5",
		$"{nameof(IgnoreAllowForTest)}7")]
	[TestCase(false,
		$"{nameof(IgnoreAllowForTest)}3",
		$"{nameof(IgnoreAllowForTest)}4",
		$"{nameof(IgnoreAllowForTest)}5",
		$"{nameof(IgnoreAllowForTest)}7")]
	public void IgnoreAllowForTest(bool isBlackList, params string[] expectedMethodNames)
	{
		string programCs =
			@$"
using Overloader;

namespace TestProject;

[{Constants.TSpecifyAttr}(typeof(double))]
[{Constants.TOverloadAttr}(typeof(float), ""Program"", ""Program1"")]
{(isBlackList ? $"[{Constants.BlackListModeAttr}]" : string.Empty)}
internal class Program
{{
	static void Main(string[] args) {{ }}

	[{Constants.ChangeModifierAttr}(""public"", ""public"")]
	[{Constants.IgnoreForAttr}]
	public static void {nameof(IgnoreAllowForTest)}1() {{ }}

	[{Constants.ChangeModifierAttr}(""public"", ""public"")]
	[{Constants.IgnoreForAttr}(typeof(float))]
	public static void {nameof(IgnoreAllowForTest)}2() {{ }}

	[{Constants.ChangeModifierAttr}(""public"", ""public"")]
	[{Constants.IgnoreForAttr}(typeof(double))]
	public static void {nameof(IgnoreAllowForTest)}3() {{ }}

	[{Constants.ChangeModifierAttr}(""public"", ""public"")]
	[{Constants.AllowForAttr}]
	public static void {nameof(IgnoreAllowForTest)}4() {{ }}

	[{Constants.ChangeModifierAttr}(""public"", ""public"")]
	[{Constants.AllowForAttr}(typeof(float))]
	public static void {nameof(IgnoreAllowForTest)}5() {{ }}

	[{Constants.ChangeModifierAttr}(""public"", ""public"")]
	[{Constants.AllowForAttr}(typeof(double))]
	public static void {nameof(IgnoreAllowForTest)}6() {{ }}

	[{Constants.ChangeModifierAttr}(""public"", ""public"")]
	[{Constants.AllowForAttr}(typeof(double))]
	[{Constants.AllowForAttr}(typeof(float))]
	[{Constants.AllowForAttr}(typeof(uint))]
	public static void {nameof(IgnoreAllowForTest)}7() {{ }}
}}
";

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
