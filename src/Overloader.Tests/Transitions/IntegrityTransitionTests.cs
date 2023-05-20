namespace Overloader.Tests.Transitions;

public class IntegrityTransitionTests
{
	[TestCase("\"new TestProject.Vector3<${T}>() { X = ${Var}.X, Y = ${Var}.Y }\"", TestName = "String literal castInBlock")]
	[TestCase("TestProject.Program.CastInBlock", TestName = "String const castInBlock")]
	[TestCase("$\"{TestProject.Program.CastInBlock}\"", TestName = "String interpolation castInBlock")]
	public void IntegrityTransitionBaseTest(string castInBlock)
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
	public const string CastInBlock = ""new TestProject.Vector3<${{T}}>() {{ X = ${{Var}}.X, Y = ${{Var}}.Y }}"";

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
		         where methodOverloads.ContainsKey(identifier)
		         select identifier)
			methodOverloads[identifier] = true;

		foreach (var kv in methodOverloads)
			Assert.That(kv.Value, Is.True);
	}
}
