using System.Text.RegularExpressions;

namespace Overloader.Tests;

[TestFixture]
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

[{nameof(TSpecify)}(typeof(double))]
[{nameof(TOverload)}(typeof(float), ""{regex}"", ""{regexReplacement}"")]
{accessModifier} static class {className} {{ }}
";
		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);
		var generatedTrees = result.Result.GeneratedTrees;
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

[assembly: {nameof(Formatter)}(
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
				""new TestProject.Vector3<${{T}}>() {{ X = ${{Var0}}.X, Y = ${{Var0}}.Y }}""
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
[assembly: {nameof(Formatter)}(
			""Vector2"",
			typeof(TestProject.Vector2<>),
			new object[] {{""T""}},
			new object[]
			{{
				""X"", ""T"",
				""Y"", ""T""
			}})]

namespace TestProject;

[{nameof(TSpecify)}(typeof(double))]
[{nameof(TOverload)}(typeof(float), ""Program"", ""Test"", ""Vector3"", ""Vector2"")]
internal class Program
{{
	static void Main(string[] args) {{ }}

	public static void TestMethod1([{TAttribute.TagName}] TestProject.Vector3<double> test) {{ }}

	public static void TestMethod2([{TAttribute.TagName}] TestProject.Vector2<double> test) {{ }}
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
		         select generatedTree.GetRoot()
			         .DescendantNodes()
			         .OfType<MethodDeclarationSyntax>()
		         into methods
		         from method in methods
		         select string.Join(',', method.ParameterList.Parameters.Select(parameter => parameter.Type!.ToString()))
		         into identifier
		         select identifier)
		{
			Assert.That(methodOverloads, Does.ContainKey(identifier));
			methodOverloads[identifier] = true;
		}

		Assert.That(methodOverloads, Does.Not.ContainValue(false));
	}

	[Test]
	public void GenericMethodOverloadTest()
	{
		const string programCs = $$"""
			using Overloader;

			namespace TestProject;

			[{{nameof(TSpecify)}}(typeof(double))]
			[{{nameof(TOverload)}}(typeof(float), "Program", "Program1")] 
			internal class Program
			{
				static void Main(string[] args) { } 
				
				[{{nameof(ChangeModifier)}}("public", "private", typeof(float))]
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
			
			[{{nameof(TSpecify)}}(typeof(double))]
			[{{nameof(TOverload)}}(typeof(float), "Test", "Test1")] 
			public class Test : ITest
			{
				[{{nameof(ForceChanged)}}]
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
	
	[Test]
	public void DetectUnknownTypeTest()
	{
		const string programCs = $$"""
			using Overloader;

			namespace TestProject;
			
			[{{nameof(TSpecify)}}(typeof(double))]
			[{{nameof(TOverload)}}(typeof(float), "Test", "Test1")] 
			public partial class Test
			{
				public void TestMethod([T] Vector2<double> test) {}
			}

			public struct Vector2<T> { }
			
			internal class Program
			{
				static void Main(string[] args) { }  
			}
		""";
		
		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationErrors, Is.Not.Empty);
	}
	
	[Test]
	public void DetectUnknownInKnownTypeTest()
	{
		const string programCs = $$"""
			using Overloader;

			[assembly: {{nameof(Formatter)}}(
				"Vector3",
				typeof(TestProject.Vector3<>),
				new object[] { "T" },
				new object[] { })]

			namespace TestProject;
			
			[{{nameof(TSpecify)}}(typeof(double), "Vector3")]
			[{{nameof(TOverload)}}(typeof(float), "Test", "Test1")] 
			public partial class Test
			{
				public void TestMethod([T] Vector3<Vector2<double>> test) {}
			}

			public struct Vector3<T> { }
			public struct Vector2<T> { }
			
			internal class Program
			{
				static void Main(string[] args) { }  
			}
		""";
		
		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationErrors, Is.Not.Empty);
	}
}
