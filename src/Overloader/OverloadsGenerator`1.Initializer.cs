using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Overloader.Entities.DTOs;
using Overloader.Exceptions;
using Overloader.Utils;
#if DEBUG && !DisableDebugger
using System.Diagnostics;
#endif

namespace Overloader;

internal sealed partial class OverloadsGenerator
{
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
	
	private sealed class SyntaxReceiver : ISyntaxReceiver
	{
		public readonly List<AttributeSyntax> GlobalFormatterSyntaxes = new(64);
		public Exception? Exception;
		public List<CandidateDto> Candidates { get; } = new(128);

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

				var typeEntry = new CandidateDto(declarationSyntax);
				foreach (var attributeList in declarationSyntax.AttributeLists)
				foreach (var attribute in attributeList.Attributes)
				{
					switch (attribute.Name.GetName())
					{
						case Constants.OverloadAttr
							when attribute.ArgumentList is not null:
						{
							var args = attribute.ArgumentList.Arguments;
							string className = declarationSyntax.Identifier.ValueText;

							string[]? formattersToUse = null;
							switch (args.Count)
							{
								case 1:
									break;
								case 2:
									throw new ArgumentException($"Need to present regex replacement parameter for {Constants.OverloadAttr}.").WithLocation(
										attribute);
								case >= 3:
									className = Regex.Replace(className, args[1].Expression.GetInnerText(), args[2].Expression.GetInnerText());
									formattersToUse = new string[args.Count - 3];
									for (int argIndex = 4, index = 0; argIndex <= args.Count; argIndex++, index++)
										formattersToUse[index] = args[argIndex].Expression.GetInnerText();
									break;
								default:
									throw new ArgumentException($"Unexpected count of args for {Constants.OverloadAttr}.").WithLocation(attribute);
							}

							typeEntry.OverloadTypes.Add(new OverloadDto(className, args[0], formattersToUse ?? Array.Empty<string>()));
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
