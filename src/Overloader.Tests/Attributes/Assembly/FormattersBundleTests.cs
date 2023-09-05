namespace Overloader.Tests.Attributes.Assembly;

[TestFixture]
public class FormattersBundleTests
{
	[Test]
	public void BaseTest()
	{
		const string programCs = 
			$$"""

			  using Overloader;

			  [assembly: {{nameof(FormattersBundle)}}(
			  			"VectorBundle",
			  			"Vector3",
			  			"Vector2")]

			  [assembly: {{nameof(Formatter)}}(
			  			"Vector3",
			  			typeof(TestProject.Vector3<>),
			  			new object[] {"T"},
			  			new object[]
			  			{
			  				"X", "T",
			  				"Y", "T",
			  				"Z", "T"
			  			})]
			  [assembly: {{nameof(Formatter)}}(
			  			"Vector2",
			  			typeof(TestProject.Vector2<>),
			  			new object[] {"T"},
			  			new object[]
			  			{
			  				"X", "T",
			  				"Y", "T"
			  			})]

			  namespace TestProject;

			  internal partial class Program
			  {
			  	static void Main(string[] args) { }
			  }

			  [{{nameof(TSpecify)}}(typeof(double), "VectorBundle")]
			  [{{nameof(TOverload)}}(typeof(float))]
			  public static partial class TestClass
			  {
			  	[return: {{TAttribute.TagName}}]
			  	public static double TestMethod1(
			  		[{{TAttribute.TagName}}] [{{nameof(Integrity)}}] this Vector3<double> vec,
			  		[{{TAttribute.TagName}}] in Vector2<double> vec1,
			  		[{{TAttribute.TagName}}] Vector3<double> vec2) => default!;
			  }

			  public struct Vector3<T>
			  {
			  	public T X;
			  	public T Y { get; set; }
			  	internal T Z { get; private set; }
			  }

			  public record struct Vector2<T>
			  {
			  	public T X;
			  	public T Y;
			  }

			  """;

		var result = GenRunner<OverloadsGenerator>.ToSyntaxTrees(programCs);
		Assert.That(result.CompilationErrors, Is.Empty);
		Assert.That(result.GenerationDiagnostics, Is.Empty);

		var methodOverloads = new Dictionary<string, bool>(3)
		{
			{"this TestProject.Vector3<double> vec,double vec1X,double vec1Y,TestProject.Vector3<double> vec2", false},
			{"this TestProject.Vector3<double> vec,in TestProject.Vector2<double> vec1,double vec2X,double vec2Y,double vec2Z", false},
			{"this TestProject.Vector3<double> vec,double vec1X,double vec1Y,double vec2X,double vec2Y,double vec2Z", false},
			{"this TestProject.Vector3<float> vec,float vec1X,float vec1Y,TestProject.Vector3<float> vec2", false},
			{"this TestProject.Vector3<float> vec,in TestProject.Vector2<float> vec1,float vec2X,float vec2Y,float vec2Z", false},
			{"this TestProject.Vector3<float> vec,float vec1X,float vec1Y,float vec2X,float vec2Y,float vec2Z", false},
			{"this TestProject.Vector3<float> vec,in TestProject.Vector2<float> vec1,TestProject.Vector3<float> vec2", false}
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
