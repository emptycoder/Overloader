using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Overloader.ChainDeclarations;
using Overloader.Entities;
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
		if (context.Compilation.Language is not "C#") return;
		if (context.SyntaxReceiver is not SyntaxReceiver syntaxReceiver
		    || !syntaxReceiver.Candidates.Any()) return;

		try
		{
#if !DEBUG || ForceTasks
			var tasks = new List<Task>();
			var taskFactory = new TaskFactory();
#endif
			var globalFormatters = syntaxReceiver.GlobalFormatterSyntaxes.GetFormatters(context.Compilation);
			foreach (var candidate in syntaxReceiver.Candidates)
			{
				string candidateClassName = candidate.Syntax.Identifier.ValueText;
				var formatters = candidate.FormatterSyntaxes.GetFormatters(context.Compilation);
				var formatterOverloadProps = new GeneratorProperties
				{
					Context = context,
					StartEntry = candidate,
					ClassName = candidateClassName,
					GlobalFormatters = globalFormatters,
					Formatters = formatters
				};
#if !DEBUG || ForceTasks
				tasks.Add(taskFactory.StartNew(OverloadCreation, formatterOverloadProps));
#else
				OverloadCreation(formatterOverloadProps);
#endif

				foreach ((string className, var argSyntax) in candidate.OverloadTypes)
				{
					var genericWithFormatterOverloadProps = new GeneratorProperties
					{
						Context = context,
						StartEntry = candidate,
						ClassName = className,
						GlobalFormatters = globalFormatters,
						Formatters = formatters,
						Template = argSyntax.GetType(context.Compilation)
					};
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
					ex.ToString(),
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
		public readonly List<AttributeSyntax> Transitions = new(64);
		public List<TypeEntrySyntax> Candidates { get; } = new(128);

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (syntaxNode is AttributeListSyntax {Target.Identifier.Text: "assembly"} attributeListSyntax)
				foreach (var attribute in attributeListSyntax.Attributes)
					switch (attribute.Name.GetName())
					{
						case Constants.FormatterAttr:
							GlobalFormatterSyntaxes.Add(attribute);
							break;
					}

			if (syntaxNode is not TypeDeclarationSyntax {AttributeLists.Count: >= 1} declarationSyntax) return;

			var typeEntry = new TypeEntrySyntax(declarationSyntax);
			bool isCandidate = false;
			foreach (var attributeList in declarationSyntax.AttributeLists)
			foreach (var attribute in attributeList.Attributes)
			{
				switch (attribute.Name.GetName())
				{
					case Constants.OverloadAttr 
				    when attribute.ArgumentList is not null :
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

						isCandidate = true;
						break;
					}
					case Constants.BlackListModeAttr:
						typeEntry.IsBlackListMode = true;
						break;
					case Constants.FormatterAttr:
						typeEntry.FormatterSyntaxes.Add(attribute);
						break;
				}
			}

			if (isCandidate) Candidates.Add(typeEntry);
		}
	}
}
