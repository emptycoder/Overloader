namespace Overloader.Tests.Attributes.Method;

[TestFixture]
public class SkipModeAttributeTests
{
	[Test]
	public void InvertedModeTest()
	{
		const string programCs = @$"
using Overloader;

namespace TestProject;

[{TSpecify.TagName}(typeof(double))]
[{TOverload.TagName}(typeof(float), ""Program"", ""Program1"")]
[{InvertedMode.TagName}]
internal class Program
{{
	static void Main(string[] args) {{ }}

	[{ChangeModifier.TagName}(""public"", ""private"", templateTypeFor: typeof(float))]
	[{ChangeModifier.TagName}(""private"", ""protected"")]
	public static void {nameof(InvertedModeTest)}() {{ }}
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
	public void IgnoreAllowForTest(bool isInvertedMode, params string[] expectedMethodNames)
	{
		string programCs =
			@$"
using Overloader;

namespace TestProject;

[{TSpecify.TagName}(typeof(double))]
[{TOverload.TagName}(typeof(float), ""Program"", ""Program1"")]
{(isInvertedMode ? $"[{InvertedMode.TagName}]" : string.Empty)}
internal class Program
{{
	static void Main(string[] args) {{ }}

	[{ChangeModifier.TagName}(""public"", ""public"")]
	[{SkipMode.TagName}(true)]
	public static void {nameof(IgnoreAllowForTest)}1() {{ }}

	[{ChangeModifier.TagName}(""public"", ""public"")]
	[{SkipMode.TagName}(true, templateTypeFor: typeof(float))]
	public static void {nameof(IgnoreAllowForTest)}2() {{ }}

	[{ChangeModifier.TagName}(""public"", ""public"")]
	[{SkipMode.TagName}(true, templateTypeFor: typeof(double))]
	public static void {nameof(IgnoreAllowForTest)}3() {{ }}

	[{ChangeModifier.TagName}(""public"", ""public"")]
	[{SkipMode.TagName}(false)]
	public static void {nameof(IgnoreAllowForTest)}4() {{ }}

	[{ChangeModifier.TagName}(""public"", ""public"")]
	[{SkipMode.TagName}(false, templateTypeFor: typeof(float))]
	public static void {nameof(IgnoreAllowForTest)}5() {{ }}

	[{ChangeModifier.TagName}(""public"", ""public"")]
	[{SkipMode.TagName}(true)]
	[{SkipMode.TagName}(false, templateTypeFor: typeof(double))]
	public static void {nameof(IgnoreAllowForTest)}6() {{ }}

	[{ChangeModifier.TagName}(""public"", ""public"")]
	[{SkipMode.TagName}(false, templateTypeFor: typeof(double))]
	[{SkipMode.TagName}(false, templateTypeFor: typeof(float))]
	[{SkipMode.TagName}(false, templateTypeFor: typeof(uint))]
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
