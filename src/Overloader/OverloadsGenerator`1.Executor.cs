using Microsoft.CodeAnalysis;
using Overloader.ChainDeclarations;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader;

[Generator]
internal sealed partial class OverloadsGenerator : ISourceGenerator
{
	private static readonly Action<object> OverloadCreation = obj =>
	{
		using var gsb = (GeneratorProperties) obj;
		gsb.Builder.AppendWoTrim(Constants.DefaultHeader);
		if (Chains.Main.Execute(gsb, gsb.StartEntry.Syntax) != ChainAction.Break)
			gsb.ReleaseAsOutput();
	};

	public void Execute(GeneratorExecutionContext context)
	{
		if (context.Compilation.Language is not "C#" ||
		    context.SyntaxReceiver is not SyntaxReceiver syntaxReceiver) return;

		try
		{
			if (syntaxReceiver.Exception is not null) throw syntaxReceiver.Exception;
			if (!syntaxReceiver.Candidates.Any()) return;

#if !DEBUG || ForceTasks
			var tasks = new List<Task>();
#endif
			var globalFormatters = syntaxReceiver.GlobalFormatterSyntaxes.GetFormattersByName(context.Compilation);
			foreach (var candidate in syntaxReceiver.Candidates)
			{
				string candidateClassName = candidate.Syntax.Identifier.ValueText;
				var formatters = globalFormatters.GetFormattersSample(candidate.FormattersToUse, candidate.Syntax);

				var formatterOverloadProps = new GeneratorProperties(
					context,
					candidate,
					formatters,
					true,
					candidateClassName,
					candidate.DefaultType!.GetType(context.Compilation),
					null);
#if !DEBUG || ForceTasks
				tasks.Add(Task.Factory.StartNew(OverloadCreation, formatterOverloadProps));
#else
				OverloadCreation(formatterOverloadProps);
#endif

				foreach (var overloadDto in candidate.OverloadTypes)
				{
					var genericWithFormatterOverloadProps = new GeneratorProperties(
						context,
						candidate,
						formatters,
						false,
						overloadDto.ClassName,
						overloadDto.TypeSyntax.GetType(context.Compilation),
						globalFormatters.GetFormattersSample(overloadDto.FormattersToUse, overloadDto.TypeSyntax)
					);

#if !DEBUG || ForceTasks
					tasks.Add(Task.Factory.StartNew(OverloadCreation, genericWithFormatterOverloadProps));
#else
					OverloadCreation(genericWithFormatterOverloadProps);
#endif
				}
			}

#if !DEBUG || ForceTasks
			tasks.ForEach(task => task.Wait());
#endif
		}
		catch (LocationException ex)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				new DiagnosticDescriptor(
					$"{nameof(Overloader)[0]}-0001",
					$"An {nameof(DiagnosticSeverity.Error)} was thrown by {nameof(Overloader)}",
					ex.InnerException!.ToString(),
					nameof(Overloader),
					DiagnosticSeverity.Error,
					true),
				ex.Location));
		}
		catch (Exception ex)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				new DiagnosticDescriptor(
					$"{nameof(Overloader)[0]}-0002",
					$"An {nameof(DiagnosticSeverity.Error)} was thrown by {nameof(Overloader)}",
					ex.ToString(),
					nameof(Overloader),
					DiagnosticSeverity.Error,
					true),
				Location.None));
		}
	}
}
