using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Overloader.Tests.GeneratorRunner;

public static class GenRunner<T> where T : ISourceGenerator, new()
{
	private static readonly T GeneratorCached = new();

	public static GenerationResult ToSyntaxTrees(params string[] sources) =>
		RunGenerator(CreateCompilation(sources));

	private static Compilation CreateCompilation(params string[] sources)
	{
		string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		string netstandardLibPath = $@"{userFolder}\.nuget\packages\netstandard.library\2.0.0\build\netstandard2.0\ref\netstandard.dll";
		if (!File.Exists(netstandardLibPath))
			throw new ArgumentException($".netstandard 2.0 not found using next path: {netstandardLibPath}");

		return CSharpCompilation.Create(
			"compilation",
			sources.Select(source => CSharpSyntaxTree.ParseText(source)),
			new[]
			{
				MetadataReference.CreateFromFile(typeof(OverloadsGenerator).Assembly.Location),
				MetadataReference.CreateFromFile(netstandardLibPath)
			},
			new CSharpCompilationOptions(OutputKind.ConsoleApplication)
		);
	}

	private static GenerationResult RunGenerator(Compilation compilation)
	{
		var genDriver = CSharpGeneratorDriver
			.Create(GeneratorCached)
			.RunGeneratorsAndUpdateCompilation(
				compilation,
				out var outputCompilation,
				out var generationDiagnostics
			);

		return new GenerationResult(
			outputCompilation,
			outputCompilation.GetDiagnostics(),
			generationDiagnostics,
			genDriver.GetRunResult()
		);
	}
}
