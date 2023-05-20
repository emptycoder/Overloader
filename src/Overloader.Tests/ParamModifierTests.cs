﻿namespace Overloader.Tests;

public class ParamModifierTests
{
	[Test]
	public void ParamModifierBaseTest()
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

internal partial class Program
{{
	static void Main(string[] args) {{ }}
}}

[{nameof(TSpecify)}(typeof(double), ""Vector3"", ""Vector2"")]
[{nameof(TOverload)}(typeof(float))]
public static partial class TestClass
{{
	public static void TestMethod1([{nameof(Integrity)}][{nameof(T)}] Vector3<double> vec, Vector3<double> vec1) {{ }}
	[return: {nameof(T)}]
	public static double TestMethod2(
		[{nameof(T)}] [{nameof(ParamModifier)}(""ref"", ""in"", typeof(Vector2<>))] this in Vector3<double> vec,
		[{nameof(T)}] [{nameof(CombineWith)}(""vec"")] [{nameof(ParamModifier)}(""in"", null, typeof(Vector2<>))] Vector3<double> vec1) => default!;
	public static void TestMethod3(Vector3<double> vec, [{nameof(T)}] double vec1) {{ }}
}}

public struct Vector3<T>
{{
	public T X;
	public T Y {{ get; set; }}
	internal T Z {{ get; private set; }}
}}

public record struct Vector2<T>
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
			{"TestProject.Vector2<double> vec,Vector3<double> vec1", false},
			{"double vecX,double vecY,double vecZ,double vec1X,double vec1Y,double vec1Z", false},
			{"double vecX,double vecY,double vecZ", false},
			{"TestProject.Vector2<double> vec0,double vecZ,TestProject.Vector2<double> vec10,double vec1Z", false},
			{"TestProject.Vector2<double> vec0,double vecZ", false},
			{"this in TestProject.Vector3<double> vec", false},
			{"this ref TestProject.Vector2<double> vec,TestProject.Vector3<double> vec1", false},
			{"this in TestProject.Vector3<double> vec,in TestProject.Vector2<double> vec1", false},
			{"this ref TestProject.Vector2<double> vec,in TestProject.Vector2<double> vec1", false},
			{"this ref TestProject.Vector2<double> vec", false},
			{"TestProject.Vector3<float> vec,Vector3<double> vec1", false},
			{"TestProject.Vector2<float> vec,Vector3<double> vec1", false},
			{"float vecX,float vecY,float vecZ,float vec1X,float vec1Y,float vec1Z", false},
			{"float vecX,float vecY,float vecZ", false},
			{"TestProject.Vector2<float> vec0,float vecZ,TestProject.Vector2<float> vec10,float vec1Z", false},
			{"TestProject.Vector2<float> vec0,float vecZ", false},
			{"this in TestProject.Vector3<float> vec,TestProject.Vector3<float> vec1", false},
			{"this in TestProject.Vector3<float> vec", false},
			{"this ref TestProject.Vector2<float> vec,TestProject.Vector3<float> vec1", false},
			{"this in TestProject.Vector3<float> vec,in TestProject.Vector2<float> vec1", false},
			{"this ref TestProject.Vector2<float> vec,in TestProject.Vector2<float> vec1", false},
			{"this ref TestProject.Vector2<float> vec", false},
			{"Vector3<double> vec,float vec1", false}
		};

		foreach (string? identifier in from generatedTree in result.Result.GeneratedTrees
		         select generatedTree.GetRoot()
			         .DescendantNodes()
			         .OfType<MethodDeclarationSyntax>()
		         into methods
		         from method in methods
		         select string.Join(',', method.ParameterList.Parameters.Select(parameter => parameter.ToString()))
		         into identifier
		         where methodOverloads.ContainsKey(identifier)
		         select identifier)
			methodOverloads[identifier] = true;

		foreach (var kv in methodOverloads)
			Assert.That(kv.Value, Is.True);
	}
}
