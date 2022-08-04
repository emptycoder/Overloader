using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations;
using Overloader.Entities;
using Overloader.Utils;

#if DEBUG
using System.Diagnostics;
#endif

namespace Overloader;

[Generator]
internal sealed class OverloadsGenerator : ISourceGenerator
{
	public void Initialize(GeneratorInitializationContext context)
	{
#if DEBUG
		if (!Debugger.IsAttached) Debugger.Launch();
#endif
		context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
	}

	public void Execute(GeneratorExecutionContext context)
	{
		// TODO: Check for compilation lang
		if (context.SyntaxReceiver is not SyntaxReceiver syntaxReceiver
		    || !syntaxReceiver.Candidates.Any()) return;

		var tasks = new List<Task>();
		var taskFactory = new TaskFactory();
		var globalFormatters = syntaxReceiver.GlobalFormatterSyntaxes.GetFormatters(context.Compilation);
		foreach (var candidate in syntaxReceiver.Candidates)
		{
			string candidateClassName = candidate.Syntax.Identifier.ValueText;
			var formatters = candidate.FormatterSyntaxes.GetFormatters(context.Compilation);
			tasks.Add(taskFactory.StartNew(obj =>
				{
					using var props = (GeneratorSourceBuilder<TypeEntrySyntax>) obj;
					Chains.Main.Execute(props);
					props.AddToContext();
				},
				new GeneratorSourceBuilder<TypeEntrySyntax>
				{
					Context = context,
					Entry = candidate,
					ClassName = candidateClassName,
					GlobalFormatters = globalFormatters,
					Formatters = formatters
				}));
			foreach ((string className, var argSyntax) in candidate.OverloadTypes)
				tasks.Add(taskFactory.StartNew(obj =>
					{
						using var props = (GeneratorSourceBuilder<TypeEntrySyntax>) obj;
						Chains.Main.Execute(props);
						props.AddToContext();
					},
					new GeneratorSourceBuilder<TypeEntrySyntax>
					{
						Context = context,
						Entry = candidate,
						ClassName = className,
						GlobalFormatters = globalFormatters,
						Formatters = formatters,
						Template = argSyntax.GetType(context.Compilation)
					}));
		}

		tasks.ForEach(task => task.Wait());
	}

	private sealed class SyntaxReceiver : ISyntaxReceiver
	{
		public readonly List<AttributeSyntax> GlobalFormatterSyntaxes = new();
		public List<TypeEntrySyntax> Candidates { get; } = new();

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (syntaxNode is AttributeListSyntax attributeListSyntax)
				foreach (var attribute in attributeListSyntax.Attributes)
					if (attribute.Name.GetName() == AttributeNames.CustomOverloadAttr)
						GlobalFormatterSyntaxes.Add(attribute);

			if (syntaxNode is not TypeDeclarationSyntax {AttributeLists.Count: >= 1} declarationSyntax) return;

			var typeEntry = new TypeEntrySyntax(declarationSyntax);
			foreach (var attributeList in declarationSyntax.AttributeLists)
			foreach (var attribute in attributeList.Attributes)
			{
				string attrName = attribute.Name.GetName();
				if (attrName == AttributeNames.PartialOverloadsAttr
				    && attribute.ArgumentList is not null
				    && attribute.ArgumentList.Arguments.Count > 0)
				{
					foreach (var typeAttributeSyntax in attribute.ArgumentList.Arguments)
						typeEntry.OverloadTypes.Add((declarationSyntax.Identifier.ValueText, typeAttributeSyntax));
				}
				else if (attrName == AttributeNames.NewClassOverloadsAttr
				         && attribute.ArgumentList is not null
				         && attribute.ArgumentList.Arguments.Count > 2)
				{
					var args = attribute.ArgumentList.Arguments;
					string className = Regex.Replace(declarationSyntax.Identifier.ValueText,
						args[0].Expression.GetInnerText(),
						args[1].Expression.GetInnerText());

					typeEntry.OverloadTypes.Add((className, args[2]));
				}
				else if (attrName == AttributeNames.CustomOverloadAttr)
				{
					typeEntry.FormatterSyntaxes.Add(attribute);
					continue;
				}
				else continue;

				Candidates.Add(typeEntry);
			}
		}
	}
}
