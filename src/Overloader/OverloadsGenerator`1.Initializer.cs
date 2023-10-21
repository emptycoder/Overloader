using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities.DTOs;
using Overloader.Exceptions;
using Overloader.Utils;
#if DEBUG && !DisableDebugger
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
					(ctx, _) => GetCandidates(ctx))
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

	private static CandidateDto? GetCandidates(
		GeneratorSyntaxContext context)
	{
		var declarationSyntax = (TypeDeclarationSyntax) context.Node;
		var typeEntry = new CandidateDto(declarationSyntax);
		foreach (var attributeList in declarationSyntax.AttributeLists)
		foreach (var attribute in attributeList.Attributes)
		{
			switch (attribute.Name.GetName())
			{
				case nameof(TOverload)
					when attribute.ArgumentList is not null:
				{
					var args = attribute.ArgumentList.Arguments;
					string className = declarationSyntax.Identifier.ValueText;

					string[]? formattersToUse = null;
					switch (args.Count)
					{
						case 1:
							break;
						case 2 when args[1].NameColon is {Name.Identifier.ValueText: "formatters"}:
							formattersToUse = new string[args.Count - 1];
							for (int argIndex = 1, index = 0; argIndex < args.Count; argIndex++, index++)
								formattersToUse[index] = args[argIndex].Expression.GetInnerText();
							break;
						case 2:
							throw new ArgumentException($"Need to present regex replacement parameter for {nameof(TOverload)}.").WithLocation(
								attribute);
						case >= 3:
							switch (args[1].Expression)
							{
								case LiteralExpressionSyntax {RawKind: (int) SyntaxKind.NullLiteralExpression} when
									args[1].Expression is LiteralExpressionSyntax {RawKind: (int) SyntaxKind.NullLiteralExpression}:
									break;
								case LiteralExpressionSyntax {RawKind: (int) SyntaxKind.StringLiteralExpression} when
									args[1].Expression is LiteralExpressionSyntax {RawKind: (int) SyntaxKind.StringLiteralExpression}:
									className = Regex.Replace(className,
										args[1].Expression.GetInnerText(),
										args[2].Expression.GetInnerText());
									break;
								default:
									throw new ArgumentException($"Argument should be null or {nameof(SyntaxKind.StringLiteralExpression)}.").WithLocation(
										args[1].Expression);
							}

							formattersToUse = new string[args.Count - 3];
							for (int argIndex = 3, index = 0; argIndex < args.Count; argIndex++, index++)
								formattersToUse[index] = args[argIndex].Expression.GetInnerText();
							break;
						default:
							throw new ArgumentException($"Unexpected count of args for {nameof(TOverload)}.").WithLocation(attribute);
					}

					typeEntry.OverloadTypes.Add(new OverloadDto(className, args[0], formattersToUse ?? Array.Empty<string>()));
					break;
				}
				case nameof(IgnoreTransitions):
					typeEntry.IgnoreTransitions = true;
					break;
				case nameof(BlackListMode):
					typeEntry.IsBlackListMode = true;
					break;
				case nameof(TSpecify):
					if (attribute.ArgumentList is not {Arguments: var arguments}
					    || arguments.Count < 1)
						throw new ArgumentException("Count of arguments must greater or equals to 1.")
							.WithLocation(declarationSyntax);
					if (arguments[0].Expression is not TypeOfExpressionSyntax type)
						throw new ArgumentException($"Argument should be {nameof(TypeOfExpressionSyntax)}.")
							.WithLocation(declarationSyntax);

					int count = arguments.Count - 1;
					typeEntry.FormattersToUse = count == 0 ? Array.Empty<string>() : new string[count];
					for (int argIndex = 1, formatterIndex = 0; argIndex < arguments.Count; argIndex++, formatterIndex++)
					{
						if (arguments[argIndex].Expression is not LiteralExpressionSyntax literal)
							throw new ArgumentException($"Formatter identifier should be {nameof(LiteralExpressionSyntax)}.")
								.WithLocation(arguments[argIndex].Expression);

						typeEntry.FormattersToUse[formatterIndex] = literal.GetInnerText();
					}

					typeEntry.DefaultType = type.Type;
					continue;
			}
		}

		return typeEntry.DefaultType is null ? null : typeEntry;
	}
}
