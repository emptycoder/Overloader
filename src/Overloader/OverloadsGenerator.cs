﻿using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Utils;
#if DEBUG
using System.Diagnostics;
#endif

namespace Overloader;

[Generator]
internal sealed class OverloadsGenerator : ISourceGenerator
{
	private static readonly Action<object> OverloadCreation = obj =>
	{
		var props = (GeneratorSourceBuilder) obj;
		if (Chains.Main.Execute(props) != ChainResult.BreakChain)
			props.AddToContext();
		props.Store.Dispose();
	};

	public void Initialize(GeneratorInitializationContext context)
	{
#if DEBUG
		if (!Debugger.IsAttached) Debugger.Launch();
#endif
		context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
	}

	public void Execute(GeneratorExecutionContext context)
	{
		if (context.Compilation.Language is not "C#") return;
		if (context.SyntaxReceiver is not SyntaxReceiver syntaxReceiver
		    || !syntaxReceiver.Candidates.Any()) return;

		try
		{
#if !DEBUG
			var tasks = new List<Task>();
			var taskFactory = new TaskFactory();
#endif
			var globalFormatters = syntaxReceiver.GlobalFormatterSyntaxes.GetFormatters(context.Compilation);
			foreach (var candidate in syntaxReceiver.Candidates)
			{
				string candidateClassName = candidate.Syntax.Identifier.ValueText;
				var formatters = candidate.FormatterSyntaxes.GetFormatters(context.Compilation);
				var formatterOverloadProps = new GeneratorSourceBuilder
				{
					Context = context,
					Entry = candidate,
					ClassName = candidateClassName,
					GlobalFormatters = globalFormatters,
					Formatters = formatters
				};
#if DEBUG
				OverloadCreation(formatterOverloadProps);
#else
				tasks.Add(taskFactory.StartNew(OverloadCreation, formatterOverloadProps));
#endif

				foreach ((string className, var argSyntax) in candidate.OverloadTypes)
				{
					var genericWithFormatterOverloadProps = new GeneratorSourceBuilder
					{
						Context = context,
						Entry = candidate,
						ClassName = className,
						GlobalFormatters = globalFormatters,
						Formatters = formatters,
						Template = argSyntax.GetType(context.Compilation)
					};
#if DEBUG
					OverloadCreation(genericWithFormatterOverloadProps);
#else
					tasks.Add(taskFactory.StartNew(OverloadCreation, genericWithFormatterOverloadProps));
#endif
				}
			}

#if !DEBUG
			tasks.ForEach(task => task.Wait());
#endif
		}
		catch (Exception ex)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				new DiagnosticDescriptor(
					$"{nameof(Overloader)[0]}-0001",
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
		public readonly List<AttributeSyntax> GlobalFormatterSyntaxes = new();
		public List<TypeEntrySyntax> Candidates { get; } = new();

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (syntaxNode is AttributeListSyntax {Target.Identifier.Text: "assembly"} attributeListSyntax)
				foreach (var attribute in attributeListSyntax.Attributes)
					if (attribute.Name.GetName() == AttributeNames.FormatterAttr)
						GlobalFormatterSyntaxes.Add(attribute);

			if (syntaxNode is not TypeDeclarationSyntax {AttributeLists.Count: >= 1} declarationSyntax) return;

			var typeEntry = new TypeEntrySyntax(declarationSyntax);
			foreach (var attributeList in declarationSyntax.AttributeLists)
			foreach (var attribute in attributeList.Attributes)
			{
				string attrName = attribute.Name.GetName();
				if (attrName == AttributeNames.OverloadsAttr
				    && attribute.ArgumentList is not null
				    && attribute.ArgumentList.Arguments.Count > 0)
				{
					var args = attribute.ArgumentList.Arguments;
					string className = declarationSyntax.Identifier.ValueText;
					className = args.Count switch
					{
						1 => className,
						2 => throw new ArgumentException($"Must be set regex replacement parameter for {nameof(OverloadAttribute)}."),
						3 => Regex.Replace(className, args[1].Expression.GetInnerText(), args[2].Expression.GetInnerText()),
						_ => throw new ArgumentException($"Unexpected count of args for {nameof(OverloadAttribute)}.")
					};

					typeEntry.OverloadTypes.Add((className, args[0]));
				}
				else if (attrName == AttributeNames.BlackListModeAttr)
				{
					typeEntry.IsBlackListMode = true;
				}
				else if (attrName == AttributeNames.FormatterAttr)
				{
					typeEntry.FormatterSyntaxes.Add(attribute);
				}
			}
			
			Candidates.Add(typeEntry);
		}
	}
}
