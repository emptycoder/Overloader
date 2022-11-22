using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Overloader.Tests;

public class OverloadAttributeTests
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

[{Constants.TSpecifyAttr}(typeof(double))]
[{Constants.OverloadAttr}(typeof(float), ""{regex}"", ""{regexReplacement}"")]
{accessModifier} static class {className} {{ }}
";
		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);
		var generatedTrees = result.Result.GeneratedTrees.Where(tree =>
			!Path.GetFileName(tree.FilePath).Equals($"{Constants.AttributesFileNameWoExt}.g.cs")).ToImmutableArray();
		Assert.That(generatedTrees, Has.Length.EqualTo(1));

		string newClassName = Regex.Replace(className, regex, regexReplacement);
		var classes = generatedTrees[0].GetRoot()
			.DescendantNodes()
			.OfType<ClassDeclarationSyntax>()
			.ToArray();
		Assert.That(classes, Has.Length.EqualTo(1));

		var @class = classes.First();
		Assert.That(@class.Identifier.Text, Is.EqualTo(newClassName));

		for (int index = 0; index < expectedModifiers.Length; index++)
			Assert.That(expectedModifiers[index], Is.EqualTo(@class.Modifiers[index].Text));
	}

	[Test]
	public void OverloadFormatters()
	{
		const string programCs = @$"
using Overloader;

[assembly: {Constants.FormatterAttr}(
			""Vector3"",
			typeof(TestProject.Vector3<>),
			new object[] {{""T""}},
			new object[]
			{{
				""X"", ""T"",
				""Y"", ""T"",
				""Z"", ""T""
			}},
			new object[]
			{{
				typeof(TestProject.Vector2<>),
				""new TestProject.Vector3<${{T}}>() {{ X = ${{Var}}.X, Y = ${{Var}}.Y }}""
			}},
			new object[]
			{{
				typeof(TestProject.Vector2<>),
				new object[]
				{{
					""X"", ""X"",
					""Y"", ""Y""
				}}
			}})]
[assembly: {Constants.FormatterAttr}(
			""Vector2"",
			typeof(TestProject.Vector2<>),
			new object[] {{""T""}},
			new object[]
			{{
				""X"", ""T"",
				""Y"", ""T""
			}})]

namespace TestProject;

[{Constants.TSpecifyAttr}(typeof(double))]
[{Constants.OverloadAttr}(typeof(float), null, null, ""Vector3"", ""Vector2"")]
internal partial class Program
{{
	public const string CastInBlock = ""new TestProject.Vector3<${{T}}>() {{ X = ${{Var}}.X, Y = ${{Var}}.Y }}"";

	static void Main(string[] args) {{ }}

	public static void TestMethod1([{Constants.TAttr}] TestProject.Vector3<double> test) {{ }}

	public static void TestMethod2([{Constants.TAttr}] TestProject.Vector2<double> test) {{ }}
}}

internal struct Vector3<T>
{{
	public T X;
	public T Y {{ get; set; }}
	internal T Z {{ get; private set; }}
}}

internal record struct Vector2<T>
{{
	public T X;
	public T Y;
}}
";

		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);

		var methodOverloads = new Dictionary<string, bool>(3)
		{
			{"float,float,float", false},
			{"TestProject.Vector2<float>,float", false},
			{"TestProject.Vector3<float>", false},
			{"TestProject.Vector2<float>", false},
			{"float,float", false}
		};

		foreach (string? identifier in from generatedTree in result.Result.GeneratedTrees
		         where !Path.GetFileName(generatedTree.FilePath).Equals($"{Constants.AttributesFileNameWoExt}.g.cs")
		         select generatedTree.GetRoot()
			         .DescendantNodes()
			         .OfType<MethodDeclarationSyntax>()
		         into methods
		         from method in methods
		         select string.Join(',', method.ParameterList.Parameters.Select(parameter => parameter.Type!.ToString()))
		         into identifier
		         where methodOverloads.ContainsKey(identifier)
		         select identifier)
			methodOverloads[identifier] = true;

		foreach (var kv in methodOverloads)
			Assert.That(kv.Value, Is.True);
	}

	[Test]
	public void GenericMethodOverloadTest()
	{
		const string programCs = $$"""
			using Overloader;

			namespace TestProject;

			[{{Constants.TSpecifyAttr}}(typeof(double))]
			[{{Constants.OverloadAttr}}(typeof(float), "Program", "Program1")] 
			internal class Program
			{
				static void Main(string[] args) { } 
				
				[{{Constants.ChangeModifierAttr}}("public", "private", typeof(float))]
				public static void Test<T>(T test) {}
			}
		""";
		
		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);
	}
	
	[Test]
	public void GenericParameterMatchingTest()
	{
		const string programCs = $$"""
			using Overloader;

			namespace TestProject;
			
			public interface ITest
			{
				public void TestMethod<T>(T test) where T: class;
			}
			
			[{{Constants.TSpecifyAttr}}(typeof(double))]
			[{{Constants.OverloadAttr}}(typeof(float), "Test", "Test1")] 
			public class Test : ITest
			{
				[{{Constants.ForceChangedAttr}}]
				public void TestMethod<T>(T test) where T: class {}
			}
			
			internal class Program
			{
				static void Main(string[] args) { }  
			}
		""";
		
		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);
	}
}
