using System.Text.RegularExpressions;
using Overloader.Tests.GeneratorRunner;
using Overloader.Tests.Utils;

namespace Overloader.Tests;

public class Tests
{
	[Test]
	public void NewClassOverloadTest()
	{
		const string className = "Vector2DExtension";
		const string regex = "2D";
		const string regexReplacement = "2F";
		const string programCs =
			@$"
using Overloader;

namespace TestProject;

internal class Program
{{
	static void Main(string[] args) {{ }}
}}

[NewClassOverload(""{regex}"", ""{regexReplacement}"", typeof(float))]
public static partial class {className}
{{
}}
";
		var result = GenRunner<OverloadsGenerator>.ToAssembly(programCs);
		string newClassName = Regex.Replace(className, regex, regexReplacement);
		Assert.Multiple(() =>
		{
			Assert.That(result.Assembly?.FindClassByName(newClassName) is not null, Is.True);
			Assert.That(result.CompilationErrors, Is.Empty);
			Assert.That(result.GenerationDiagnostics, Is.Empty);
		});
	}
	
	
}
