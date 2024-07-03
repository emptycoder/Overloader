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

[{nameof(TSpecify)}(typeof(double))]
[{nameof(TOverload)}(typeof(float), ""Program"", ""Program1"")]
[{nameof(InvertedMode)}]
internal class Program
{{
	static void Main(string[] args) {{ }}

	[{nameof(ChangeModifier)}(""public"", ""private"", templateTypeFor: typeof(float))]
	[{nameof(ChangeModifier)}(""private"", ""protected"")]
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

[{nameof(TSpecify)}(typeof(double))]
[{nameof(TOverload)}(typeof(float), ""Program"", ""Program1"")]
{(isInvertedMode ? $"[{nameof(InvertedMode)}]" : string.Empty)}
internal class Program
{{
	static void Main(string[] args) {{ }}

	[{nameof(ChangeModifier)}(""public"", ""public"")]
	[{nameof(SkipMode)}(true)]
	public static void {nameof(IgnoreAllowForTest)}1() {{ }}

	[{nameof(ChangeModifier)}(""public"", ""public"")]
	[{nameof(SkipMode)}(true, templateTypeFor: typeof(float))]
	public static void {nameof(IgnoreAllowForTest)}2() {{ }}

	[{nameof(ChangeModifier)}(""public"", ""public"")]
	[{nameof(SkipMode)}(true, templateTypeFor: typeof(double))]
	public static void {nameof(IgnoreAllowForTest)}3() {{ }}

	[{nameof(ChangeModifier)}(""public"", ""public"")]
	[{nameof(SkipMode)}(false)]
	public static void {nameof(IgnoreAllowForTest)}4() {{ }}

	[{nameof(ChangeModifier)}(""public"", ""public"")]
	[{nameof(SkipMode)}(false, templateTypeFor: typeof(float))]
	public static void {nameof(IgnoreAllowForTest)}5() {{ }}

	[{nameof(ChangeModifier)}(""public"", ""public"")]
	[{nameof(SkipMode)}(true)]
	[{nameof(SkipMode)}(false, templateTypeFor: typeof(double))]
	public static void {nameof(IgnoreAllowForTest)}6() {{ }}

	[{nameof(ChangeModifier)}(""public"", ""public"")]
	[{nameof(SkipMode)}(false, templateTypeFor: typeof(double))]
	[{nameof(SkipMode)}(false, templateTypeFor: typeof(float))]
	[{nameof(SkipMode)}(false, templateTypeFor: typeof(uint))]
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
