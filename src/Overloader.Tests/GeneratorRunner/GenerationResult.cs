﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Overloader.Tests.GeneratorRunner;

public record GenerationResult(
	Compilation Compilation,
	ImmutableArray<Diagnostic> CompilationDiagnostics,
	ImmutableArray<Diagnostic> GenerationDiagnostics,
	GeneratorDriverRunResult Result
)
{
	public ImmutableArray<Diagnostic> CompilationErrors =>
		[
			..CompilationDiagnostics
				.Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Error)
		];

	public ImmutableArray<Diagnostic> GenerationErrors =>
		[
			..GenerationDiagnostics
				.Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Error)
		];
}
