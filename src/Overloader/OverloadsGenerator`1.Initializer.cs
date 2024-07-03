using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities.DTOs;
using Overloader.Exceptions;
#if DEBUG && !DisableDebugger
using System.Diagnostics;
#endif

namespace Overloader;

public sealed partial class OverloadsGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
#if DEBUG && !DisableDebugger
		// if (Debugger.IsAttached) Debugger.Break();
#endif
		try
		{
			var candidatesProvider = context.SyntaxProvider
				.CreateSyntaxProvider(
					(s, _) => s is TypeDeclarationSyntax {AttributeLists.Count: >= 1},
					(ctx, _) => CandidateDto.TryParse(ctx, out var candidateDto) ? candidateDto : null)
				.Where(candidate => candidate.HasValue);

			var globalAttributes = context.SyntaxProvider
				.CreateSyntaxProvider(
					(s, _) => s is AttributeListSyntax {Target.Identifier.RawKind: (int) SyntaxKind.AssemblyKeyword},
					(ctx, _) => Enumerable.FirstOrDefault(((AttributeListSyntax) ctx.Node).Attributes))
				.Where(item => item is not null);

			context.RegisterSourceOutput(context.CompilationProvider
					.Combine(candidatesProvider.Collect())
					.Combine(globalAttributes.Collect()),
				(ctx, t) =>
					Execute(ctx, t.Left.Left, t.Left.Right, t.Right));
		}
		catch (Exception ex)
		{
			context.RegisterSourceOutput(context.CompilationProvider,
				(ctx, _) => ReportErrorDuringInitialize(ctx, ex));
		}
	}

	public static void ReportErrorDuringInitialize(
		SourceProductionContext context,
		Exception ex)
	{
		switch (ex)
		{
			case LocationException locationException:
				context.ReportDiagnostic(Diagnostic.Create(
					new DiagnosticDescriptor(
						$"{nameof(Overloader)[0]}I0001",
						$"An {nameof(DiagnosticSeverity.Error)} was thrown by {nameof(Overloader)} during Analyzing",
						ex.InnerException!.ToString(),
						nameof(Overloader),
						DiagnosticSeverity.Error,
						true),
					locationException.Location));
				break;
			default:
				context.ReportDiagnostic(Diagnostic.Create(
					new DiagnosticDescriptor(
						$"{nameof(Overloader)[0]}I0002",
						$"An {nameof(DiagnosticSeverity.Error)} was thrown by {nameof(Overloader)} during Analyzing",
						ex.ToString(),
						nameof(Overloader),
						DiagnosticSeverity.Error,
						true),
					Location.None));
				break;
		}
	}
}
