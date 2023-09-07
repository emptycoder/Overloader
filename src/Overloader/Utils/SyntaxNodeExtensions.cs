using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Exceptions;

namespace Overloader.Utils;

public static class SyntaxNodeExtensions
{
	public static string? GetNamespace(this SyntaxNode syntax)
	{
		// If we don't have a namespace at all we'll return an empty string
		// This accounts for the "default namespace" case
		string? nameSpace = null;

		// Get the containing syntax node for the type declaration
		// (could be a nested type, for example)
		var potentialNamespaceParent = syntax;

		// Keep moving "out" of nested classes etc until we get to a namespace
		// or until we run out of parents
		while (potentialNamespaceParent is not NamespaceDeclarationSyntax
		       && potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
		{
			var parent = potentialNamespaceParent.Parent;
			if (parent is null) break;
			potentialNamespaceParent = parent;
		}

		// Build up the final namespace by looping until we no longer have a namespace declaration
		NameSpace:
		switch (potentialNamespaceParent)
		{
			case BaseNamespaceDeclarationSyntax namespaceParent:
			{
				// We have a namespace. Use that as the type
				nameSpace = namespaceParent.Name.ToString();

				// Keep moving "out" of the namespace declarations until we 
				// run out of nested namespace declarations
				while (true)
				{
					if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
					{
						break;
					}

					// Add the outer namespace as a prefix to the final namespace
					nameSpace = $"{namespaceParent.Name}.{nameSpace}";
					namespaceParent = parent;
				}

				break;
			}
			case CompilationUnitSyntax unitSyntax:
			{
				foreach (var member in unitSyntax.Members)
				{
					if (member is not (NamespaceDeclarationSyntax or FileScopedNamespaceDeclarationSyntax)) continue;
					potentialNamespaceParent = member;
					goto NameSpace;
				}

				break;
			}
		}

		// return the final namespace
		return nameSpace;
	}

	public static string GetName(this NameSyntax nameSyntax) => nameSyntax switch
	{
		// [name]
		IdentifierNameSyntax identifierSyntax => identifierSyntax.Identifier.ValueText,
		// [namespace.name]
		QualifiedNameSyntax qualifiedNameSyntax => qualifiedNameSyntax.Right.Identifier.ValueText,
		_ => throw new ArgumentException("Unknown attribute.").WithLocation(nameSyntax)
	};

	public static SyntaxNode GetTopParent(this SyntaxNode syntax)
	{
		var topNode = syntax;
		while (topNode.Parent is not null)
			topNode = topNode.Parent;

		return topNode;
	}

	public static ITypeSymbol GetType(this AttributeArgumentSyntax type, Compilation compilation) =>
		GetType(type.Expression, compilation);

	public static ITypeSymbol GetType(this ParameterSyntax type, Compilation compilation) =>
		GetType(type.Type!, compilation);

	public static ITypeSymbol GetType(this CSharpSyntaxNode syntaxNode, Compilation compilation)
	{
		if (syntaxNode is TypeOfExpressionSyntax typeOfExpressionSyntax)
			syntaxNode = typeOfExpressionSyntax.Type;

		if (syntaxNode is RefTypeSyntax refTypeSyntax)
			syntaxNode = refTypeSyntax.Type;

		var semanticModel = compilation.GetSemanticModel(syntaxNode.SyntaxTree);
		return semanticModel.GetTypeInfo(syntaxNode).Type ?? throw new ArgumentException(
			$"Type not found or {syntaxNode.ToFullString()} isn't type.").WithLocation(syntaxNode);
	}

	public static bool EqualsToTemplate<T>(this AttributeArgumentSyntax arg, T props) where T : IGeneratorProps =>
		SymbolEqualityComparer.Default.Equals(arg.GetType(props.Compilation), props.Template);

	public static string GetStringValue(this SyntaxNode syntaxNode, Compilation compilation)
	{
		switch (syntaxNode)
		{
			case LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression):
				return literal.GetVariableName();
			case MemberAccessExpressionSyntax:
			case InterpolatedStringExpressionSyntax:
				var value = compilation
					.GetSemanticModel(syntaxNode.SyntaxTree)
					.GetConstantValue(syntaxNode);

				if (!value.HasValue || value.Value is not string) goto default;
				return (string) value.Value!;
			default:
				throw new ArgumentException("SyntaxNode isn't StringLiteral/InterpolationString/MemberAccess.")
					.WithLocation(syntaxNode);
		}
	}

	public static string GetVariableName(this SyntaxNode syntaxNode)
	{
		string name;
		switch (syntaxNode)
		{
			case LiteralExpressionSyntax literalExpression:
				name = literalExpression.Kind() switch
				{
					SyntaxKind.StringLiteralExpression => literalExpression.GetInnerText(),
					SyntaxKind.NumericLiteralExpression => literalExpression.ToString(),
					_ => throw new ArgumentException($"Literal ({literalExpression.ToString()}) isn't allowed.")
						.WithLocation(syntaxNode)
				};
				break;
			case InvocationExpressionSyntax invocationExpressionSyntax
				when invocationExpressionSyntax.Expression.IsKind(SyntaxKind.IdentifierName):
				var args = invocationExpressionSyntax.ArgumentList.Arguments;
				if (args.Count != 1)
					throw new ArgumentException("args.Count != 1.")
						.WithLocation(invocationExpressionSyntax);

				name = args[0].Expression switch
				{
					MemberAccessExpressionSyntax syntax => syntax.Name.Identifier.Text,
					IdentifierNameSyntax syntax => syntax.Identifier.Text,
					_ => throw new ArgumentException($"Expression isn't {nameof(MemberAccessExpressionSyntax)}.")
						.WithLocation(invocationExpressionSyntax)
				};
				break;
			default:
				throw new ArgumentException("Expression isn't literal or nameof syntax.")
					.WithLocation(syntaxNode);
		}

		return name;
	}
}
