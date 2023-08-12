namespace Overloader.Tests.Transitions;

[TestFixture]
public class CastTransitionTests
{
	[TestCase("\"new TestProject.Vector3<${T}>() { X = ${Var0}.X, Y = ${Var0}.Y }\"", TestName = "String literal castInBlock")]
	[TestCase("TestProject.Program.CastInBlock", TestName = "String const castInBlock")]
	[TestCase("$\"{TestProject.Program.CastInBlock}\"", TestName = "String interpolation castInBlock")]
	public void BaseTest(string castInBlock)
	{
		string programCs = @$"
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
				{castInBlock}
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

[{nameof(TSpecify)}(typeof(double), ""Vector3"", ""Vector2"")]
[{nameof(TOverload)}(typeof(float))]
internal partial class Program
{{
	public const string CastInBlock = ""new TestProject.Vector3<${{T}}>() {{ X = ${{Var0}}.X, Y = ${{Var0}}.Y }}"";

	static void Main(string[] args) {{ }}

	public static void TestMethod1([{nameof(Integrity)}][{TAttribute.TagName}] Vector3<double> vec, Vector3<double> vec1) {{ }}

	[return: {TAttribute.TagName}]
	public static double TestMethod2(
		[{TAttribute.TagName}] Vector3<double> vec,
		[{TAttribute.TagName}][{nameof(CombineWith)}(""vec"")] Vector3<double> vec1)
	{{
		Test(vec);
		//# ""double"" -> ""${{T}}""
		return (double) (vec.X + vec1.X + vec.Y + vec1.Y + vec.Z + vec1.Z);
	}}

	private static void Test(Vector3<double> vec123) {{}}
	private static void Test(Vector3<float> vec123) {{}}
	private static void Test(double x, double y, double z) {{}}
	private static void Test(float x, float y, float z) {{}}

	public static void TestMethod3(Vector3<double> vec, [{TAttribute.TagName}] double vec1) {{ }}
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
			{"TestProject.Vector2<double>,Vector3<double>", false},
			{"double,double,double,double,double,double", false},
			{"double,double,double", false},
			{"TestProject.Vector2<double>,double,TestProject.Vector2<double>,double", false},
			{"TestProject.Vector2<double>,double", false},
			{"TestProject.Vector3<double>", false},
			{"TestProject.Vector2<double>,TestProject.Vector3<double>", false},
			{"TestProject.Vector3<double>,TestProject.Vector2<double>", false},
			{"TestProject.Vector2<double>,TestProject.Vector2<double>", false},
			{"TestProject.Vector2<double>", false},
			{"TestProject.Vector3<float>,Vector3<double>", false},
			{"TestProject.Vector2<float>,Vector3<double>", false},
			{"float,float,float,float,float,float", false},
			{"float,float,float", false},
			{"TestProject.Vector2<float>,float,TestProject.Vector2<float>,float", false},
			{"TestProject.Vector2<float>,float", false},
			{"TestProject.Vector3<float>,TestProject.Vector3<float>", false},
			{"TestProject.Vector3<float>", false},
			{"TestProject.Vector2<float>,TestProject.Vector3<float>", false},
			{"TestProject.Vector3<float>,TestProject.Vector2<float>", false},
			{"TestProject.Vector2<float>,TestProject.Vector2<float>", false},
			{"TestProject.Vector2<float>", false},
			{"Vector3<double>,float", false}
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
	public void RefIgnoringTest()
	{
		const string programCs = @$"
using Overloader;

[assembly: {nameof(Formatter)}(
			""Vector3D"",
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
				TransitionType.Cast,
				typeof(TestProject.Vector2<float>),
				""new TestProject.Vector3<${{T}}>() {{ X = (${{T}}) ${{Var0}}.X, Y = (${{T}}) ${{Var0}}.Y }}""
			}},
			new object[]
			{{
				TransitionType.Cast,
				typeof(TestProject.Vector3<long>),
				""new TestProject.Vector3<${{T}}>() {{ X = (${{T}}) ${{Var0}}.X, Y = (${{T}}) ${{Var0}}.Y }}""
			}},
			new object[]
			{{
				TransitionType.Cast,
				typeof(long),
				typeof(long),
				""new TestProject.Vector3<${{T}}>() {{ X = (${{T}}) ${{Var0}}, Y = (${{T}}) ${{Var1}} }}""
			}},
			new object[]
			{{
				TransitionType.Decomposition,
				typeof(TestProject.Vector2<>),
				new object[]
				{{
					""X"", ""X"",
					""Y"", ""Y""
				}}
			}})]
[assembly: {nameof(Formatter)}(
			""Vector3F"",
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
				TransitionType.Cast,
				typeof(TestProject.Vector2<double>),
				""new TestProject.Vector3<${{T}}>() {{ X = (${{T}}) ${{Var0}}.X, Y = (${{T}}) ${{Var0}}.Y }}""
			}},
			new object[]
			{{
				TransitionType.Decomposition,
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

[{nameof(TSpecify)}(typeof(double), ""Vector3D"", ""Vector2"")]
[{nameof(TOverload)}(typeof(float), formatters: ""Vector3F"")]
internal partial class Program
{{
	static void Main(string[] args) {{ }}

	public static void TestMethod1([{nameof(Integrity)}][{TAttribute.TagName}] Vector3<double> vec0, Vector3<double> vec1) {{ }}

	[return: {TAttribute.TagName}]
	public static double TestMethod2(
		[{TAttribute.TagName}][{nameof(Integrity)}] ref Vector3<double> vec0,
		[{TAttribute.TagName}][{nameof(CombineWith)}(""vec0"")] Vector3<double> vec1)
	{{
		return vec0.X;
	}}

	public static void TestMethod3(Vector3<double> vec0, [{TAttribute.TagName}] double vec1) {{ }}
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
			{"TestProject.Vector2<float>,Vector3<double>", false},
			{"TestProject.Vector3<long>,Vector3<double>", false},
			{"long,long,Vector3<double>", false},
			{"TestProject.Vector3<double>,double,double,double", false},
			{"TestProject.Vector3<double>,TestProject.Vector2<double>,double", false},
			{"TestProject.Vector3<double>", false},
			{"TestProject.Vector3<double>,TestProject.Vector2<float>", false},
			{"TestProject.Vector3<double>,TestProject.Vector3<long>", false},
			{"TestProject.Vector3<double>,long,long", false},
			{"TestProject.Vector3<float>,Vector3<double>", false},
			{"TestProject.Vector2<double>,Vector3<double>", false},
			{"TestProject.Vector3<float>,float,float,float", false},
			{"TestProject.Vector3<float>,TestProject.Vector2<float>,float", false},
			{"TestProject.Vector3<float>,TestProject.Vector3<float>", false},
			{"TestProject.Vector3<float>", false},
			{"TestProject.Vector3<float>,TestProject.Vector2<double>", false},
			{"Vector3<double>,float", false}
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
	public void GetFormattersWithSpecifiedGenericsTest()
	{
		const string programCs = @$"
using Overloader;

[assembly: {nameof(Formatter)}(
			""Vector3D"",
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
				typeof(TestProject.Vector3<float>),
				""new TestProject.Vector3<double>() {{ X = (double) ${{Var0}}.X, Y = (double) ${{Var0}}.Y }}""
			}})]
[assembly: {nameof(Formatter)}(
			""Vector3F"",
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
				typeof(TestProject.Vector3<double>),
				""new TestProject.Vector3<float>() {{ X = (float) ${{Var0}}.X, Y = (float) ${{Var0}}.Y }}""
			}})]

namespace TestProject;

[{nameof(TSpecify)}(typeof(double), ""Vector3D"")]
[{nameof(TOverload)}(typeof(float), formatters: ""Vector3F"")]
internal partial class Program
{{
	static void Main(string[] args) {{ }}

	public static void TestMethod1(
		[{TAttribute.TagName}] [{nameof(Integrity)}] ref Vector3<double> vec0,
		[{TAttribute.TagName}] Vector3<double> vec1) {{ }}
}}

internal struct Vector3<T>
{{
	public T X;
	public T Y {{ get; set; }}
	internal T Z {{ get; private set; }}
}}
";

		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);
	}
}
