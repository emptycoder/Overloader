using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Overloader.Entities;

public sealed class GeneratorSourceBuilder<T> : SourceBuilder, IGeneratorProps, IDisposable
{
	private TypeSyntax? _typeSyntax;
	public StoreDictionary Store { get; private init; } = new();
	public Dictionary<ITypeSymbol, Formatter> Formatters { get; init; } = default!;
	public Dictionary<ITypeSymbol, Formatter> GlobalFormatters { get; init; } = default!;
	public T Entry { get; init; } = default!;
	public GeneratorExecutionContext Context { private get; init; }
	public void Dispose() => Store.Dispose();
	public string ClassName { get; init; } = default!;
	public ITypeSymbol? Template { get; init; }

	public TypeSyntax? TemplateSyntax => _typeSyntax ??=
		Template is not null ? SyntaxFactory.ParseTypeName(Template.Name) : default;

	public Compilation Compilation => Context.Compilation;

	public void AddToContext() => Context.AddSource(ClassName, ToString());

	public GeneratorSourceBuilder<TEntry> With<TEntry>(TEntry entry) => new()
	{
		Formatters = Formatters,
		GlobalFormatters = GlobalFormatters,
		Entry = entry,
		ClassName = ClassName,
		Template = Template,
		Context = Context,
		Store = Store
	};

	public GeneratorSourceBuilder<T> WriteMethodBody(MethodDeclarationSyntax method, IList<(string From, string To)>? replaceModifiers)
	{
		// Body
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
				string strStatement;
				switch (statement)
				{
					// case LocalDeclarationStatementSyntax localDeclarationStatementSyntax:
					// 	mainSb.Append(string.Concat(statement.GetLeadingTrivia().ToFullString(), 
					// 		localDeclarationStatementSyntax.Declaration.WithType(SyntaxFactory.ParseTypeName("double ")).ToFullString(),
					// 		localDeclarationStatementSyntax.SemicolonToken.ToFullString()));
					// 	continue;
					// case ExpressionStatementSyntax expressionStatementSyntax:
					// 	if (expressionStatementSyntax.Expression is not AssignmentExpressionSyntax assignmentExpressionSyntax) goto default;
					// _sb.Append(string.Concat(assignmentExpressionSyntax.Left.ToFullString(),
					// 	" = (", type.Name, ") (",
					// 	assignmentExpressionSyntax.Right.ToFullString(),
					// 	")",
					// 	expressionStatementSyntax.SemicolonToken.ToFullString()));
					// 	continue;
					// case ReturnStatementSyntax returnStatementSyntax:
					// 	if (method.ReturnType is not PredefinedTypeSyntax) goto default;
					// _sb.Append(string.Concat(returnStatementSyntax.ReturnKeyword.ToFullString(),
					// 	"(", type.Name, ") (",
					// 	returnStatementSyntax.Expression?.ToFullString() ?? string.Empty,
					// 	")",
					// 	returnStatementSyntax.SemicolonToken.ToFullString()));
					// continue;
					default:
						strStatement = statement.ToFullString();
						break;
				}

				// TODO: Create multiple replacer
				if (replaceModifiers is not null)
					foreach ((string from, string to) in replaceModifiers)
						strStatement = strStatement.Replace(from, to);

				Append(strStatement);
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
