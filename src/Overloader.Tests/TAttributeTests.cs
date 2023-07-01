namespace Overloader.Tests;

[TestFixture]
// ReSharper disable once InconsistentNaming
public class TAttributeTests
{
	[Test]
	// ReSharper disable once InconsistentNaming
	public void TSpecifyTOverloadTest()
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
				""Y"", typeof(double),
				""Z"", new[]
				{{
					typeof(float), typeof(double),
					typeof(double), typeof(long)
				}}
			}})]

namespace TestProject;

[{nameof(TSpecify)}(typeof(double), ""Vector3"")]
[{nameof(TOverload)}(typeof(float))]
internal partial class Program
{{
	static void Main(string[] args) {{ }}

	public static void {nameof(TSpecifyTOverloadTest)}1(
		[{nameof(Integrity)}][{TAttribute.TagName}] Vector3<double> vec,
		Vector3<double> vec1) {{ }}

	[return: {TAttribute.TagName}]
	public static double {nameof(TSpecifyTOverloadTest)}2(
		[{TAttribute.TagName}] Vector3<double> vec,
		[{TAttribute.TagName}] Vector3<double> vec1)
	{{
		Test(vec);
		//# ""double"" -> ""${{T}}""
		return (double) (vec.X + vec1.X + vec.Y + vec1.Y + vec.Z + vec1.Z);
	}}

	[return: {TAttribute.TagName}]
	public static double {nameof(TSpecifyTOverloadTest)}3([{TAttribute.TagName}] double[] vec)
	{{
		return vec[0] + vec[1] + vec[3];
	}}

	[return: {TAttribute.TagName}]
	public static Vector3<double>[] {nameof(TSpecifyTOverloadTest)}3([{nameof(Integrity)}][{TAttribute.TagName}] Vector3<double>[] vec)
	{{
		// Check that auto integrity works
		var test = vec[0].X + vec[1].X;
		return vec;
	}}

	private static void Test(Vector3<double> vec123) {{}}
	private static void Test(Vector3<float> vec123) {{}}
	private static void Test(double x, double y, double z) {{}}
	private static void Test(float x, float y, float z) {{}}

	public static void {nameof(TSpecifyTOverloadTest)}4(Vector3<double> vec, [{TAttribute.TagName}] double vec1) {{ }}
}}

internal struct Vector3<T>
{{
	public double X;
	public T Y {{ get; set; }}
	internal T Z {{ get; private set; }}
}}
";

		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);

		var methodOverloads = new Dictionary<string, bool>(4)
		{
			{"double,double,long,double,double,long", false},
			{"TestProject.Vector3<float>,Vector3<double>", false},
			{"float,double,double,float,double,double", false},
			{"TestProject.Vector3<float>,TestProject.Vector3<float>", false},
			{"float[]", false},
			{"TestProject.Vector3<float>[]", false},
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
	// ReSharper disable once InconsistentNaming
	public void TParamTest()
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
				""Y"", typeof(double),
				""Z"", new[]
				{{
					typeof(float), typeof(double),
					typeof(double), typeof(long)
				}}
			}})]

namespace TestProject;

[{nameof(TSpecify)}(typeof(double), ""Vector3"")]
[{nameof(TOverload)}(typeof(float))]
internal partial class Program
{{
	static void Main(string[] args) {{ }}

	public static void {nameof(TSpecifyTOverloadTest)}1(
		[{nameof(Integrity)}][{TAttribute.TagName}(typeof(int), typeof(float))] Vector3<double> vec,
		Vector3<double> vec1) {{ }}
}}

internal struct Vector3<T>
{{
	public double X;
	public T Y {{ get; set; }}
	internal T Z {{ get; private set; }}
}}
";

		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);
		
		var methodOverloads = new Dictionary<string, bool>(4)
		{
			{"int,Vector3<double>", false}
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
}
