namespace Overloader.Tests.Attributes.Parameter;

[TestFixture]
public class RefTests
{
	[Test]
	public void BaseTest()
	{
		const string programCs = @$"
using Overloader;

[assembly: {Formatter.TagName}(
			""Vector3"",
			typeof(TestProject.Vector3<>),
			new object[] {{""T""}},
			new object[]
			{{
				""X"", ""T"",
				""Y"", ""T"",
				""Z"", ""T""
			}})]

namespace TestProject;

internal partial class Program
{{
	static void Main(string[] args) {{ }}
}}

[{TSpecify.TagName}(typeof(double), ""Vector3"")]
[{TOverload.TagName}(typeof(float))]
public static partial class TestClass
{{
	[return: {TAttribute.TagName}]
	public static double TestMethod1(
		[{TAttribute.TagName}] [{Integrity.TagName}] [{Ref.TagName}] this Vector3<double> vec,
		[{TAttribute.TagName}] [{Integrity.TagName}] [{Ref.TagName}] in Vector3<double> vec1,
		[{TAttribute.TagName}] [{Integrity.TagName}] Vector3<double> vec2,
		Vector3<double> vec3,
		[{TAttribute.TagName}] [{Ref.TagName}] [{CombineWith.TagName}(""vec1"")] Vector3<double> vec4,
		Vector3<double> vec5) => default!;

	[return: {TAttribute.TagName}]
	public static ref int TestMethod2(
		[{TAttribute.TagName}] this ref int value,
		[{TAttribute.TagName}] [{Ref.TagName}] [{Integrity.TagName}] Vector3<double> vec2) => ref value;
}}

public struct Vector3<T>
{{
	public T X;
	public T Y {{ get; set; }}
	internal T Z {{ get; private set; }}
}}
";

		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);

		var methodOverloads = new Dictionary<string, bool>
		{
			{"this TestProject.Vector3<double> vec,in TestProject.Vector3<double> vec1,TestProject.Vector3<double> vec2,Vector3<double> vec3,double vec4X,double vec4Y,double vec4Z,Vector3<double> vec5", false},
			{"this TestProject.Vector3<double> vec,in TestProject.Vector3<double> vec1,TestProject.Vector3<double> vec2,Vector3<double> vec3,Vector3<double> vec5", false},
			{"this ref TestProject.Vector3<double> vec,in TestProject.Vector3<double> vec1,TestProject.Vector3<double> vec2,Vector3<double> vec3,TestProject.Vector3<double> vec4,Vector3<double> vec5", false},
			{"this TestProject.Vector3<double> vec,in TestProject.Vector3<double> vec1,TestProject.Vector3<double> vec2,Vector3<double> vec3,ref TestProject.Vector3<double> vec4,Vector3<double> vec5", false},
			{"this ref TestProject.Vector3<double> vec,in TestProject.Vector3<double> vec1,TestProject.Vector3<double> vec2,Vector3<double> vec3,ref TestProject.Vector3<double> vec4,Vector3<double> vec5", false},
			{"this ref TestProject.Vector3<double> vec,in TestProject.Vector3<double> vec1,TestProject.Vector3<double> vec2,Vector3<double> vec3,Vector3<double> vec5", false},
			{"this TestProject.Vector3<float> vec,in TestProject.Vector3<float> vec1,TestProject.Vector3<float> vec2,Vector3<double> vec3,float vec4X,float vec4Y,float vec4Z,Vector3<double> vec5", false},
			{"this TestProject.Vector3<float> vec,in TestProject.Vector3<float> vec1,TestProject.Vector3<float> vec2,Vector3<double> vec3,TestProject.Vector3<float> vec4,Vector3<double> vec5", false},
			{"this TestProject.Vector3<float> vec,in TestProject.Vector3<float> vec1,TestProject.Vector3<float> vec2,Vector3<double> vec3,Vector3<double> vec5", false},
			{"this ref TestProject.Vector3<float> vec,in TestProject.Vector3<float> vec1,TestProject.Vector3<float> vec2,Vector3<double> vec3,TestProject.Vector3<float> vec4,Vector3<double> vec5", false},
			{"this TestProject.Vector3<float> vec,in TestProject.Vector3<float> vec1,TestProject.Vector3<float> vec2,Vector3<double> vec3,ref TestProject.Vector3<float> vec4,Vector3<double> vec5", false},
			{"this ref TestProject.Vector3<float> vec,in TestProject.Vector3<float> vec1,TestProject.Vector3<float> vec2,Vector3<double> vec3,ref TestProject.Vector3<float> vec4,Vector3<double> vec5", false},
			{"this ref TestProject.Vector3<float> vec,in TestProject.Vector3<float> vec1,TestProject.Vector3<float> vec2,Vector3<double> vec3,Vector3<double> vec5", false},
			{"this ref double value,ref TestProject.Vector3<double> vec2", false},
			{"this ref float value,TestProject.Vector3<float> vec2", false},
			{"this ref float value,ref TestProject.Vector3<float> vec2", false}
		};

		foreach (string? identifier in from generatedTree in result.Result.GeneratedTrees
		         select generatedTree.GetRoot()
			         .DescendantNodes()
			         .OfType<MethodDeclarationSyntax>()
		         into methods
		         from method in methods
		         select string.Join(',', method.ParameterList.Parameters.Select(parameter => parameter.ToString()))
		         into identifier
		         select identifier)
		{
			Assert.That(methodOverloads, Does.ContainKey(identifier));
			methodOverloads[identifier] = true;
		}

		Assert.That(methodOverloads, Does.Not.ContainValue(false));
	}
}
