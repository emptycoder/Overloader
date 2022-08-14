using System.Text.RegularExpressions;
using Overloader.Tests.GeneratorRunner;
using Overloader.Tests.Utils;

namespace Overloader.Tests;

public class OverloadAttributesTest
{
	[TestCase("public", ExpectedResult = true)]
	[TestCase("internal", ExpectedResult = false)]
	public bool NewClassOverloadTest(string modifier)
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

[NewClassOverload(""{regex}"", ""{regexReplacement}"", typeof(float))]
{modifier} static partial class {className}
{{
}}
";
		var result = GenRunner<OverloadsGenerator>.ToAssembly(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);
		string newClassName = Regex.Replace(className, regex, regexReplacement);
		var newClass = result.Assembly?.FindClassByName(newClassName);

		Assert.That(newClass is not null, Is.True);

		return newClass!.IsPublic;
	}
}
