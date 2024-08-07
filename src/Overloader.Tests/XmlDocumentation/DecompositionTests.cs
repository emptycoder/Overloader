using Overloader.Tests.Utils;

namespace Overloader.Tests.XmlDocumentation;

[TestFixture]
public class DecompositionTests
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
			  
			    /// jkjsdkjfjsk<summary>
			    /// Summary
			    /// </summary>
			    /// <param name="vec">description of param 0</param>
			    /// <param name="vec1">description of param 1</param>
			   public static void TestMethod1([{{Integrity.TagName}}][{{TAttribute.TagName}}] Vector3<double> vec, Vector3<double> vec1) { }
			  
			    /// jkjsdkjfjsk<summary>
			    /// Summary
			    /// </summary>
			    /// <param name="vec">description of param 0</param>
			    /// <param name="vec1">description of param 1</param>
			    /// <returns>test</returns>
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

		// All trivia was extruded from float overload
		var expectedTrivia = new Dictionary<string, bool>()
		{
			{
				"""
				// Generated by: IntegrityOverload
				/// jkjsdkjfjsk <summary>
				/// Summary
				/// </summary>
				/// <param name="vec">description of param 0</param>
				/// <param name="vec1">description of param 1</param>
				""".Minify(),
				false
			},
			{
				"""
				// Generated by: DecompositionOverloads
				/// jkjsdkjfjsk <summary>
				/// Summary
				/// </summary>
				/// <param name="vecX">description of param 0</param>
				/// <param name="vecY">description of param 0</param>
				/// <param name="vecZ">description of param 0</param>
				/// <param name="vec1">description of param 1</param>
				/// <returns>test</returns>
				""".Minify(),
				false
			},
			{
				"""
				// Generated by: DecompositionOverloads
				/// jkjsdkjfjsk <summary>
				/// Summary
				/// </summary>
				/// <param name="vec">description of param 0</param>
				/// <param name="vec1X">description of param 1</param>
				/// <param name="vec1Y">description of param 1</param>
				/// <param name="vec1Z">description of param 1</param>
				/// <returns>test</returns>
				""".Minify(),
				false
			},
			{
				"""
				// Generated by: DecompositionOverloads
				/// jkjsdkjfjsk <summary>
				/// Summary
				/// </summary>
				/// <param name="vecX">description of param 0</param>
				/// <param name="vecY">description of param 0</param>
				/// <param name="vecZ">description of param 0</param>
				/// <param name="vec1X">description of param 1</param>
				/// <param name="vec1Y">description of param 1</param>
				/// <param name="vec1Z">description of param 1</param>
				/// <returns>test</returns>
				""".Minify(),
				false
			},
			{
				"""
				// Generated by: CombinedDecompositionOverloads
				/// jkjsdkjfjsk <summary>
				/// Summary
				/// </summary>
				/// <param name="vecX">description of param 0</param>
				/// <param name="vecY">description of param 0</param>
				/// <param name="vecZ">description of param 0</param>
				/// <returns>test</returns>
				""".Minify(),
				false
			},
			{
				"""
				// Generated by: IntegrityOverload
				/// jkjsdkjfjsk <summary>
				/// Summary
				/// </summary>
				/// <param name="vec">description of param 0</param>
				/// <param name="vec1">description of param 1</param>
				/// <returns>test</returns>
				""".Minify(),
				false
			},
			{
				"""
				// Generated by: CombinedIntegrityOverload
				/// jkjsdkjfjsk <summary>
				/// Summary
				/// </summary>
				/// <param name="vec">description of param 0</param>
				/// <returns>test</returns>
				""".Minify(),
				false
			},
			{
				"""
				// Generated by: DecompositionTransitionOverloads
				/// jkjsdkjfjsk <summary>
				/// Summary
				/// </summary>
				/// <param name="vec0">description of param 0</param>
				/// <param name="vecZ">description of param 0</param>
				/// <param name="vec10">description of param 1</param>
				/// <param name="vec1Z">description of param 1</param>
				/// <returns>test</returns>
				""".Minify(),
				false
			},
			{
				"""
				// Generated by: CombinedDecompositionTransitionOverloads
				/// jkjsdkjfjsk <summary>
				/// Summary
				/// </summary>
				/// <param name="vec0">description of param 0</param>
				/// <param name="vecZ">description of param 0</param>
				/// <returns>test</returns>
				""".Minify(),
				false
			}
		};
		
		foreach (string trivia in from generatedTree in result.Result.GeneratedTrees
		         select generatedTree.GetRoot()
			         .DescendantNodes()
			         .OfType<MethodDeclarationSyntax>()
		         into methods
		         from method in methods
		         select method.GetLeadingTrivia().ToFullString()
		         into trivia
		         select trivia)
		{
			var minifiedTrivia = trivia.Minify();
			Assert.That(expectedTrivia, Does.ContainKey(minifiedTrivia));
			expectedTrivia[minifiedTrivia] = true;
		}
		
		Assert.That(expectedTrivia, Does.Not.ContainValue(false));
	}
}
