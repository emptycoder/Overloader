using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Overloader.Tests.GeneratorRunner;

public record GenerationResultWithSyntaxTree(
	Compilation Compilation,
	ImmutableArray<Diagnostic> CompilationDiagnostics,
	ImmutableArray<Diagnostic> GenerationDiagnostics,
	GeneratorDriverRunResult Result) : GenerationResult(Compilation, CompilationDiagnostics, GenerationDiagnostics) { }
