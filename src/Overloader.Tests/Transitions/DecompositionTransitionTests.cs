﻿namespace Overloader.Tests.Transitions;

[TestFixture]
public class DecompositionTransitionTests
{
	[Test]
	public void BaseTest()
	{
		const string programCs =
			$$"""
			  using Overloader;

			  [assembly: {{Formatter.TagName}}(
			           "Vector3",
			           typeof(TestProject.Vector3<>),
			           new object[] {"T"},
			           new object[]
			           {
			               "X", "T",
			               "Y", "T",
			               "Z", "T"
			           },
			           new object[]
			           {
			               {{nameof(TransitionType)}}.{{nameof(TransitionType.Decomposition)}},
			               typeof(TestProject.Vector2<>),
			               new object[]
			               {
			                   "X", "X",
			                   "Y", "Y"
			               }
			           })]
			  [assembly: {{Formatter.TagName}}(
			           "Vector2",
			           typeof(TestProject.Vector2<>),
			           new object[] {"T"},
			           new object[]
			           {
			               "X", "T",
			               "Y", "T"
			           })]

			  namespace TestProject;

			  [{{TSpecify.TagName}}(typeof(double), "Vector3", "Vector2")]
			  [{{TOverload.TagName}}(typeof(float))]
			  internal partial class Program
			  {
			   static void Main(string[] args) { }
			  
			   public static void TestMethod1([{{Integrity.TagName}}][{{TAttribute.TagName}}] Vector3<double> vec, Vector3<double> vec1) { }
			  
			   [return: {{TAttribute.TagName}}]
			   public static double TestMethod2(
			       [{{TAttribute.TagName}}] Vector3<double> vec,
			       [{{TAttribute.TagName}}][{{CombineWith.TagName}}("vec")] Vector3<double> vec1)
			   {
			       Test(vec);
			       //# "double" -> "${T}"
			       return (double) (vec.X + vec1.X + vec.Y + vec1.Y + vec.Z + vec1.Z);
			   }
			  
			   private static void Test(Vector3<double> vec123) {}
			   private static void Test(Vector3<float> vec123) {}
			   private static void Test(double x, double y, double z) {}
			   private static void Test(float x, float y, float z) {}
			  
			   public static void TestMethod3(Vector3<double> vec, [{{TAttribute.TagName}}] double vec1) { }
			  }

			  internal struct Vector3<T>
			  {
			   public T X;
			   public T Y { get; set; }
			   internal T Z { get; private set; }
			  }

			  internal record struct Vector2<T>
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
			{"double,double,double,TestProject.Vector3<double>", false},
			{"TestProject.Vector3<double>,double,double,double", false},
			{"double,double,double,double,double,double", false},
			{"double,double,double", false},
			{"TestProject.Vector3<double>", false},
			{"TestProject.Vector2<double>,double,TestProject.Vector2<double>,double", false},
			{"TestProject.Vector2<double>,double", false},
			{"TestProject.Vector3<float>,Vector3<double>", false},
			{"float,float,float,TestProject.Vector3<float>", false},
			{"TestProject.Vector3<float>,float,float,float", false},
			{"float,float,float,float,float,float", false},
			{"float,float,float", false},
			{"TestProject.Vector3<float>,TestProject.Vector3<float>", false},
			{"TestProject.Vector3<float>", false},
			{"TestProject.Vector2<float>,float,TestProject.Vector2<float>,float", false},
			{"TestProject.Vector2<float>,float", false},
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
	public void DecompositionTest()
	{
		const string programCs = 
			$$"""

			  using Overloader;

			  [assembly: {{Formatter.TagName}}(
			  			"Vector3",
			  			typeof(TestProject.Vector3<>),
			  			new object[] {"T"},
			  			new object[]
			  			{
			  				"X", "T",
			  				"Y", "T",
			  				"Z", "T"
			  			})]
			  [assembly: {{Formatter.TagName}}(
			  			"Vector2",
			  			typeof(TestProject.Vector2<>),
			  			new object[] {"T"},
			  			new object[]
			  			{
			  				"X", "T",
			  				"Y", "T"
			  			},
			  			new object[]
			  			{
			  				{{nameof(TransitionType)}}.{{nameof(TransitionType.Decomposition)}},
			  				typeof(TestProject.Vector3<>),
			  				new object[]
			  				{
			  					"X", "X",
			  					"Y", "Y"
			  				}
			  			})]

			  namespace TestProject;

			  [{{TSpecify.TagName}}(typeof(double), "Vector3", "Vector2")]
			  [{{TOverload.TagName}}(typeof(float))]
			  internal partial class Program
			  {
			  	static void Main(string[] args) { }
			  
			  	public static void TestMethod1([{{Integrity.TagName}}][{{TAttribute.TagName}}] Vector3<double> vec, Vector3<double> vec1) { }
			  
			  	[return: {{TAttribute.TagName}}]
			  	public static double TestMethod2(
			  		[{{TAttribute.TagName}}] Vector2<double> vec,
			  		[{{TAttribute.TagName}}] in Vector3<double> vec1,
			  		[{{TAttribute.TagName}}] in Vector2<double> vec2)
			  	{
			  		//# "double" -> "${T}"
			  		return (double) (vec.X + vec1.X + vec.Y + vec1.Y );
			  	}
			  }

			  internal struct Vector3<T>
			  {
			  	public T X;
			  	public T Y { get; set; }
			  	internal T Z { get; private set; }
			  }

			  internal record struct Vector2<T>
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
			{"double,double,TestProject.Vector3<double>,TestProject.Vector2<double>", false},
			{"TestProject.Vector2<double>,double,double,double,TestProject.Vector2<double>", false},
			{"double,double,double,double,double,TestProject.Vector2<double>", false},
			{"TestProject.Vector2<double>,TestProject.Vector3<double>,double,double", false},
			{"double,double,TestProject.Vector3<double>,double,double", false},
			{"TestProject.Vector2<double>,double,double,double,double,double", false},
			{"double,double,double,double,double,double,double", false},
			{"TestProject.Vector3<double>,TestProject.Vector3<double>,double,double,double,TestProject.Vector3<double>", false},
			{"TestProject.Vector3<float>,Vector3<double>", false},
			{"float,float,TestProject.Vector3<float>,TestProject.Vector2<float>", false},
			{"TestProject.Vector2<float>,float,float,float,TestProject.Vector2<float>", false},
			{"float,float,float,float,float,TestProject.Vector2<float>", false},
			{"TestProject.Vector2<float>,TestProject.Vector3<float>,float,float", false},
			{"float,float,TestProject.Vector3<float>,float,float", false},
			{"TestProject.Vector2<float>,float,float,float,float,float", false},
			{"float,float,float,float,float,float,float", false},
			{"TestProject.Vector3<float>,TestProject.Vector3<float>,float,float,float,TestProject.Vector3<float>", false},
			{"TestProject.Vector2<float>,TestProject.Vector3<float>,TestProject.Vector2<float>", false}
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
