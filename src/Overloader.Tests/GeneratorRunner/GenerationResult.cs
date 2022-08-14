using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Overloader.Tests.GeneratorRunner;

public record GenerationResult(
	Compilation Compilation,
	ImmutableArray<Diagnostic> CompilationDiagnostics,
	ImmutableArray<Diagnostic> GenerationDiagnostics
)
{
	public GeneratorDriverRunResult? Result { get; init; }

	public ImmutableArray<Diagnostic> CompilationErrors =>
		CompilationDiagnostics
			.Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Error)
			.ToImmutableArray();

	public ImmutableArray<Diagnostic> GenerationErrors =>
		GenerationDiagnostics
			.Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Error)
			.ToImmutableArray();
}
