using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Overloader.Tests.GeneratorRunner;

public static class GenRunner<T> where T: ISourceGenerator, new()
{
	private static readonly T GeneratorCached = new();

    public static CompilationResult ToAssembly(params string[] sources)
    {
        var baseCompilation = CreateCompilation(sources);
        var (outputCompilation, compilationDiagnostics, generationDiagnostics) = RunGenerator(
            baseCompilation
        );

        using var ms = new MemoryStream();
        Assembly? assembly = null;

        try
        {
            outputCompilation.Emit(ms);
            assembly = Assembly.Load(ms.ToArray());
        }
        catch
        {
            // Do nothing since we want to inspect the diagnostics when compilation fails.
        }

        return new(
            Assembly: assembly,
            CompilationDiagnostics: compilationDiagnostics,
            GenerationDiagnostics: generationDiagnostics
        );
    }

    private static Compilation CreateCompilation(params string[] sources)
    {
	    string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
	    string netstandardLibPath = $@"{userFolder}\.nuget\packages\netstandard.library\2.0.0\build\netstandard2.0\ref\netstandard.dll";
	    if (!File.Exists(netstandardLibPath)) throw new ArgumentException($"Netstandard not found using next path: {netstandardLibPath}");
	    
	    return CSharpCompilation.Create(
		    "compilation",
		    sources.Select(source => CSharpSyntaxTree.ParseText(source)),
		    new[]
		    {
			    MetadataReference.CreateFromFile(typeof(Overloader.OverloadsGenerator).Assembly.Location),
			    MetadataReference.CreateFromFile(netstandardLibPath),
		    },
		    new CSharpCompilationOptions(OutputKind.ConsoleApplication)
	    );
    }

    private static GenerationResult RunGenerator(Compilation compilation)
    {
        CSharpGeneratorDriver
            .Create(GeneratorCached)
            .RunGeneratorsAndUpdateCompilation(
                compilation,
                out var outputCompilation,
                out var generationDiagnostics
            );

        return new(
            Compilation: outputCompilation,
            CompilationDiagnostics: outputCompilation.GetDiagnostics(),
            GenerationDiagnostics: generationDiagnostics
        );
    }
}
