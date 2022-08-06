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

#if !DEBUG
		var tasks = new List<Task>();
		var taskFactory = new TaskFactory();
#endif
		var globalFormatters = syntaxReceiver.GlobalFormatterSyntaxes.GetFormatters(context.Compilation);
		foreach (var candidate in syntaxReceiver.Candidates)
		{
			string candidateClassName = candidate.Syntax.Identifier.ValueText;
			var formatters = candidate.FormatterSyntaxes.GetFormatters(context.Compilation);
			var overloadCreation = new Action<object>(obj =>
			{
				using var props = (GeneratorSourceBuilder) obj;
				Chains.Main.Execute(props);
				props.AddToContext();
			});
			var formatterOverloadProps = new GeneratorSourceBuilder
			{
				Context = context,
				Entry = candidate,
				ClassName = candidateClassName,
				GlobalFormatters = globalFormatters,
				Formatters = formatters
			};
#if DEBUG
			overloadCreation(formatterOverloadProps);
#else
			tasks.Add(taskFactory.StartNew(overloadCreation, formatterOverloadProps));
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
				overloadCreation(genericWithFormatterOverloadProps);
#else
				tasks.Add(taskFactory.StartNew(overloadCreation, genericWithFormatterOverloadProps));
#endif
			}
		}

#if !DEBUG
		tasks.ForEach(task => task.Wait());
#endif
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
