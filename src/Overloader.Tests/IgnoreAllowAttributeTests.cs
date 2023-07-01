namespace Overloader.Tests;

[TestFixture]
public class IgnoreAllowAttributeTests
{
	[Test]
	public void BlackListModeTest()
	{
		const string programCs = @$"
using Overloader;

namespace TestProject;

[{nameof(TSpecify)}(typeof(double))]
[{nameof(TOverload)}(typeof(float), ""Program"", ""Program1"")]
[{nameof(BlackListMode)}]
internal class Program
{{
	static void Main(string[] args) {{ }}

	[{nameof(ChangeModifier)}(""public"", ""private"", typeof(float))]
	[{nameof(ChangeModifier)}(""private"", ""protected"")]
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

[{nameof(TSpecify)}(typeof(double))]
[{nameof(TOverload)}(typeof(float), ""Program"", ""Program1"")]
{(isBlackList ? $"[{nameof(BlackListMode)}]" : string.Empty)}
internal class Program
{{
	static void Main(string[] args) {{ }}

	[{nameof(ChangeModifier)}(""public"", ""public"")]
	[{nameof(IgnoreFor)}]
	public static void {nameof(IgnoreAllowForTest)}1() {{ }}

	[{nameof(ChangeModifier)}(""public"", ""public"")]
	[{nameof(IgnoreFor)}(typeof(float))]
	public static void {nameof(IgnoreAllowForTest)}2() {{ }}

	[{nameof(ChangeModifier)}(""public"", ""public"")]
	[{nameof(IgnoreFor)}(typeof(double))]
	public static void {nameof(IgnoreAllowForTest)}3() {{ }}

	[{nameof(ChangeModifier)}(""public"", ""public"")]
	[{nameof(AllowFor)}]
	public static void {nameof(IgnoreAllowForTest)}4() {{ }}

	[{nameof(ChangeModifier)}(""public"", ""public"")]
	[{nameof(AllowFor)}(typeof(float))]
	public static void {nameof(IgnoreAllowForTest)}5() {{ }}

	[{nameof(ChangeModifier)}(""public"", ""public"")]
	[{nameof(AllowFor)}(typeof(double))]
	public static void {nameof(IgnoreAllowForTest)}6() {{ }}

	[{nameof(ChangeModifier)}(""public"", ""public"")]
	[{nameof(AllowFor)}(typeof(double))]
	[{nameof(AllowFor)}(typeof(float))]
	[{nameof(AllowFor)}(typeof(uint))]
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
