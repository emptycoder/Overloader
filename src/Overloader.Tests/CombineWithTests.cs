﻿namespace Overloader.Tests;

public class CombineWithTests
{
	[Test]
	public void CombineWithBaseTest()
	{
		const string programCs =
			@$"
using System;
using Overloader;

[assembly:{nameof(Formatter)}(
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

[{nameof(TSpecify)}(typeof(double), ""Vector3"")]
[{nameof(TOverload)}(typeof(float))]
internal partial class Program
{{
	static void Main(string[] args) {{ }}

	public static void {nameof(CombineWithBaseTest)}(
		[{TAttribute.TagName}] ref Vector3<double> vec,
		[{TAttribute.TagName}][{nameof(CombineWith)}(""vec"")] Vector3<double> vec1) {{ }}
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

		var methodOverloads = new Dictionary<string, bool>(3)
		{
			{"double,double,double,double,double,double", false},
			{"double,double,double", false},
			{"float,float,float,float,float,float", false},
			{"float,float,float", false},
			{"TestProject.Vector3<float>,TestProject.Vector3<float>", false},
			{"TestProject.Vector3<float>", false}
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
