using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Formatters;
using Overloader.Utils;

namespace Overloader.Entities;

internal partial record GeneratorSourceBuilder : IGeneratorProps
{
	private TypeSyntax? _typeSyntax;

	public StoreDictionary Store { get; } = new();
	public Dictionary<ITypeSymbol, Formatter> Formatters { get; init; } = default!;
	public Dictionary<ITypeSymbol, Formatter> GlobalFormatters { get; init; } = default!;
	public object Entry { get; init; } = default!;
	public GeneratorExecutionContext Context { private get; init; }
	public string ClassName { get; init; } = default!;
	public ITypeSymbol? Template { get; init; }

	public TypeSyntax? TemplateSyntax => _typeSyntax ??=
		Template is not null ? SyntaxFactory.ParseTypeName(Template.Name) : default;

	public Compilation Compilation => Context.Compilation;
	public void AddToContext() => Context.AddSource(ClassName, ToString());

	public GeneratorSourceBuilder WriteMethodBody(MethodDeclarationSyntax method, IList<(string From, string To)>? replaceModifiers)
	{
		if (method.ExpressionBody is not null)
		{
			Append(method.ExpressionBody.ArrowToken.ToFullString())
				.Append(method.ExpressionBody.Expression.ToFullString())
				.Append(";", 1);
		}
		else if (method.Body is not null)
		{
			NestedIncrease();
			foreach (var statement in method.Body.Statements)
			{
				string strStatement = statement.WithoutLeadingTrivia().ToString();
				if (statement.HasLeadingTrivia)
					foreach (var syntaxTrivia in statement.GetLeadingTrivia())
					{
						switch (syntaxTrivia.Kind())
						{
							case SyntaxKind.SingleLineCommentTrivia:
								var strTrivia = syntaxTrivia.ToString();
								var strTriviaSpan = strTrivia.AsSpan();
								int separatorIndex = strTrivia.LastIndexOf(':');
								var templateStr = Template?.ToDisplayString();
								
								if (separatorIndex != -1)
								{
									if (Template is null) continue;
									if (templateStr != strTriviaSpan.Slice(separatorIndex + 1).Trim().ToString()) continue;
									strTriviaSpan = strTriviaSpan.Slice(0, separatorIndex);
								}
								
								switch (strTrivia[2])
								{
									// Replace operation
									case '#':
										var kv = strTriviaSpan.SplitAsKV("->");
										strStatement = strStatement.Replace(kv.Key, kv.Value.Replace("${T}", templateStr));
										break;
									// Change line operation
									case '$':
										strStatement = strTriviaSpan.Slice(3).Trim().ToString().Replace("${T}", templateStr);
										break;
									default:
										Append(strTrivia);
										break;
								}
								break;
							case SyntaxKind.WhitespaceTrivia:
								break;
							default:
								Append(syntaxTrivia.ToString());
								break;
						}
					}

				// TODO: Create multiple replacer
				if (replaceModifiers is not null)
					foreach ((string from, string to) in replaceModifiers)
						strStatement = strStatement.Replace(from, to);

				Append(strStatement, 1);
			}

			AppendLineAndNestedDecrease();
		}

		return this;
	}
}

public interface IGeneratorProps
{
	public string ClassName { get; }
	public ITypeSymbol? Template { get; }
	public TypeSyntax? TemplateSyntax { get; }
	public Compilation Compilation { get; }
}
