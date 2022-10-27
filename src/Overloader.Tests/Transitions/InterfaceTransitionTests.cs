﻿namespace Overloader.Tests.Transitions;

public class InterfaceTransitionTests
{
	[Test]
	public void InterfaceTransitionBaseTest()
	{
		const string programCs = @$"
using Overloader;

[assembly: {Constants.FormatterAttr}(typeof(TestProject.Vector3<>),
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
[assembly: {Constants.FormatterAttr}(typeof(TestProject.Vector2<>),
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

[{Constants.TSpecifyAttr}(typeof(double))]
[{Constants.OverloadAttr}(typeof(float))]
[{Constants.RemoveBodyAttr}]
internal partial interface ITest
{{
	public void TestMethod1([{Constants.IntegrityAttr}][{Constants.TAttr}] Vector3<double> vec, Vector3<double> vec1);
	[return: {Constants.TAttr}]
	public double TestMethod2(
		[{Constants.TAttr}] Vector3<double> vec,
		[{Constants.TAttr}][{Constants.CombineWithAttr}(""vec"")] Vector3<double> vec1);
	public void TestMethod3(Vector3<double> vec, [{Constants.TAttr}] double vec1);
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