using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Overloader.ChainDeclarations;
using Overloader.Entities;
using Overloader.Entities.Formatters;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;
#if DEBUG && !DisableDebugger
using System.Diagnostics;
#endif

namespace Overloader;

[Generator]
internal sealed class OverloadsGenerator : ISourceGenerator
{
	private static readonly Action<object> OverloadCreation = obj =>
	{
		using var gsb = (GeneratorProperties) obj;
		gsb.Builder.AppendWoTrim(Constants.DefaultHeader);
		if (Chains.Main.Execute(gsb, gsb.StartEntry.Syntax) != ChainAction.Break)
			gsb.ReleaseAsOutput();
	};

	public void Initialize(GeneratorInitializationContext context)
	{
#if DEBUG && !DisableDebugger
		if (!Debugger.IsAttached) Debugger.Launch();
#endif
		context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
		context.RegisterForPostInitialization(ctx =>
		{
			ctx.AddSource($"{Constants.AttributesFileNameWoExt}.g.cs",
				SourceText.From(Constants.AttributesWithHeaderSource, Encoding.UTF8));
		});
	}

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
			var taskFactory = new TaskFactory();
#endif
			var globalFormatters = syntaxReceiver.GlobalFormatterSyntaxes.GetFormattersByName(context.Compilation);
			foreach (var candidate in syntaxReceiver.Candidates)
			{
				string candidateClassName = candidate.Syntax.Identifier.ValueText;
				Dictionary<ITypeSymbol, Formatter>? formatters = null;
				if (candidate.FormattersToUse is not null)
				{
					formatters = new Dictionary<ITypeSymbol, Formatter>(candidate.FormattersToUse.Length, SymbolEqualityComparer.Default);
					foreach (string formatterIdentifier in candidate.FormattersToUse)
					{
						if (!globalFormatters.TryGetValue(formatterIdentifier, out var formatter))
							throw new ArgumentException($"Can't find formatter with identifier '{formatterIdentifier}'.")
								.WithLocation(candidate.Syntax);

						foreach (var formatterType in formatter.Types)
						{
							if (formatters.TryGetValue(formatterType, out var sameTypeFormatter))
								throw new ArgumentException($"Type has been already overridden by '{sameTypeFormatter.Identifier}' formatter.")
									.WithLocation(candidate.Syntax);
							formatters.Add(formatterType, formatter);
						}
					}
				}

				var formatterOverloadProps = new GeneratorProperties(
					context,
					candidate,
					true,
					candidateClassName,
					formatters,
					candidate.DefaultType!.GetType(context.Compilation));
#if !DEBUG || ForceTasks
				tasks.Add(taskFactory.StartNew(OverloadCreation, formatterOverloadProps));
#else
				OverloadCreation(formatterOverloadProps);
#endif

				foreach ((string className, var argSyntax) in candidate.OverloadTypes)
				{
					var genericWithFormatterOverloadProps = new GeneratorProperties(
						context,
						candidate,
						false,
						className,
						formatters,
						argSyntax.GetType(context.Compilation));
#if !DEBUG || ForceTasks
					tasks.Add(taskFactory.StartNew(OverloadCreation, genericWithFormatterOverloadProps));
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

	private sealed class SyntaxReceiver : ISyntaxReceiver
	{
		public readonly List<AttributeSyntax> GlobalFormatterSyntaxes = new(64);
		public Exception? Exception;
		public List<TypeEntrySyntax> Candidates { get; } = new(128);

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			try
			{
				if (syntaxNode is AttributeListSyntax {Target.Identifier.RawKind: (int) SyntaxKind.AssemblyKeyword} attributeListSyntax)
				{
					foreach (var attribute in attributeListSyntax.Attributes)
						switch (attribute.Name.GetName())
						{
							case Constants.FormatterAttr:
								GlobalFormatterSyntaxes.Add(attribute);
								break;
						}
				}

				if (syntaxNode is not TypeDeclarationSyntax {AttributeLists.Count: >= 1} declarationSyntax) return;

				var typeEntry = new TypeEntrySyntax(declarationSyntax);
				foreach (var attributeList in declarationSyntax.AttributeLists)
				foreach (var attribute in attributeList.Attributes)
				{
					switch (attribute.Name.GetName())
					{
						case Constants.OverloadAttr
							when attribute.ArgumentList is not null:
						{
							var args = attribute.ArgumentList.Arguments;
							if (args.Count > 0)
							{
								string className = declarationSyntax.Identifier.ValueText;
								className = args.Count switch
								{
									1 => className,
									2 => throw new ArgumentException($"Must be set regex replacement parameter for {Constants.OverloadAttr}.")
										.WithLocation(attribute),
									3 => Regex.Replace(className, args[1].Expression.GetInnerText(), args[2].Expression.GetInnerText()),
									_ => throw new ArgumentException($"Unexpected count of args for {Constants.OverloadAttr}.")
										.WithLocation(attribute)
								};

								typeEntry.OverloadTypes.Add((className, args[0]));
							}

							break;
						}
						case Constants.IgnoreTransitionsAttr:
							typeEntry.IgnoreTransitions = true;
							break;
						case Constants.BlackListModeAttr:
							typeEntry.IsBlackListMode = true;
							break;
						case Constants.TSpecifyAttr:
							if (attribute.ArgumentList is not {Arguments: var arguments}
							    || arguments.Count < 1)
								throw new ArgumentException("Count of arguments must greater or equals to 1.")
									.WithLocation(declarationSyntax);
							if (arguments[0].Expression is not TypeOfExpressionSyntax type)
								throw new ArgumentException($"Argument must be {nameof(TypeOfExpressionSyntax)}.")
									.WithLocation(declarationSyntax);

							typeEntry.FormattersToUse = new string[arguments.Count - 1];
							for (int argIndex = 1, formatterIndex = 0; argIndex < arguments.Count; argIndex++, formatterIndex++)
							{
								if (arguments[argIndex].Expression is not LiteralExpressionSyntax literal)
									throw new ArgumentException("Formatter identifier must be LiteralExpressionSyntax.")
										.WithLocation(arguments[argIndex].Expression);

								typeEntry.FormattersToUse[formatterIndex] = literal.GetInnerText();
							}

							typeEntry.DefaultType = type.Type;
							continue;
					}
				}

				if (typeEntry.DefaultType is null) return;
				Candidates.Add(typeEntry);
			}
			catch (Exception ex)
			{
				Exception = ex;
			}
		}
	}
}
