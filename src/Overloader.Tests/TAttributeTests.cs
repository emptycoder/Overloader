using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Tests.GeneratorRunner;

namespace Overloader.Tests;

// ReSharper disable once InconsistentNaming
public class TAttributeTests
{
	[Test]
	// ReSharper disable once InconsistentNaming
	public void TAttrTest()
	{
		const string programCs = @$"
using Overloader;

namespace TestProject;

[{Constants.FormatterAttr}(typeof(TestProject.Vector3<>),
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
[{Constants.OverloadAttr}(typeof(float))]
internal partial class Program
{{
	static void Main(string[] args) {{ }}

	public static void {nameof(TAttrTest)}1([Integrity][T] Vector3<double> vec, Vector3<double> vec1) {{ }}

	[return: T]
	public static double {nameof(TAttrTest)}2([T] Vector3<double> vec, [T] Vector3<double> vec1)
	{{
		Test(vec);
		//# ""double"" -> ""${{T}}""
		return (double) (vec.X + vec1.X + vec.Y + vec1.Y + vec.Z + vec1.Z);
	}}

	[return: T]
	public static double {nameof(TAttrTest)}3([T] double[] vec)
	{{
		return vec[0] + vec[1] + vec[3];
	}}

	[return: T]
	public static Vector3<double>[] {nameof(TAttrTest)}3([Integrity][T] Vector3<double>[] vec)
	{{
		// Check that auto integrity works
		var test = vec[0].X + vec[1].X;
		return vec;
	}}

	private static void Test(Vector3<double> vec123) {{}}
	private static void Test(Vector3<float> vec123) {{}}
	private static void Test(double x, double y, double z) {{}}
	private static void Test(float x, float y, float z) {{}}

	public static void {nameof(TAttrTest)}4(Vector3<double> vec, [T] double vec1) {{ }}
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
			{"float,double,double,float,double,double", false},
			{"TestProject.Vector3<float>,Vector3<double>", false},
			{"TestProject.Vector3<float>,TestProject.Vector3<float>", false},
			{"Vector3<double>,float", false}
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
}
