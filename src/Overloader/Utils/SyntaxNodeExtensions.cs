﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Overloader.Entities;

namespace Overloader.Utils;

internal static class SyntaxNodeExtensions
{
	public static string GetNamespace(this BaseTypeDeclarationSyntax syntax)
	{
		// If we don't have a namespace at all we'll return an empty string
		// This accounts for the "default namespace" case
		string nameSpace = string.Empty;

		// Get the containing syntax node for the type declaration
		// (could be a nested type, for example)
		var potentialNamespaceParent = syntax.Parent;

		// Keep moving "out" of nested classes etc until we get to a namespace
		// or until we run out of parents
		while (potentialNamespaceParent != null &&
		       potentialNamespaceParent is not NamespaceDeclarationSyntax
		       && potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
		{
			potentialNamespaceParent = potentialNamespaceParent.Parent;
		}

		// Build up the final namespace by looping until we no longer have a namespace declaration
		if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
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
		}

		// return the final namespace
		return nameSpace;
	}

	public static bool TryGetTAttr(this ParameterSyntax syntaxNode,
		IGeneratorProps props,
		out AttributeSyntax? attr)
	{
		foreach (var attrList in syntaxNode.AttributeLists)
		foreach (var attribute in attrList.Attributes)
		{
			if (!attribute.Name.ToString().Equals(AttributeNames.TAttr)) continue;
			if (attribute.ArgumentList is {Arguments.Count: > 1} &&
			    (props.Template is null || attribute.ArgumentList.Arguments[1].EqualsToTemplate(props))) continue;

			attr = attribute;
			return true;
		}

		attr = default;
		return false;
	}

	public static string GetName(this NameSyntax nameSyntax) => nameSyntax switch
	{
		// [name]
		IdentifierNameSyntax identifierSyntax => identifierSyntax.Identifier.ValueText,
		// [namespace.name]
		QualifiedNameSyntax qualifiedNameSyntax => qualifiedNameSyntax.Right.Identifier.ValueText,
		_ => throw new ArgumentException("Unknown attribute.")
	};

	public static string GetInnerText(this ExpressionSyntax expressionSyntax) =>
		expressionSyntax.GetText().GetInnerText();

	public static string GetInnerText(this SourceText sourceText) =>
		sourceText.GetSubText(new TextSpan(1, sourceText.Length - 2)).ToString();

	public static SyntaxNode GetTopParent(this SyntaxNode syntax)
	{
		var topNode = syntax;
		while (topNode.Parent is not null)
			topNode = topNode.Parent;

		return topNode;
	}

	public static ITypeSymbol GetType(this AttributeArgumentSyntax type, Compilation compilation) =>
		GetType(type.Expression, compilation);

	public static ITypeSymbol GetType(this CSharpSyntaxNode node, Compilation compilation)
	{
		if (node is TypeOfExpressionSyntax typeOfExpressionSyntax)
			node = typeOfExpressionSyntax.Type;

		var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
		return semanticModel.GetTypeInfo(node).Type ??
		       throw new ArgumentException("Type not found.");
	}

	public static Dictionary<ITypeSymbol, Formatter> GetFormatters(this IList<AttributeSyntax> attributeSyntaxes, Compilation compilation)
	{
		var dict = new Dictionary<ITypeSymbol, Formatter>(attributeSyntaxes.Count, SymbolEqualityComparer.Default);

		foreach (var formatterSyntax in attributeSyntaxes)
		{
			var args = formatterSyntax.ArgumentList?.Arguments ??
			           throw new ArgumentException("Argument list can't be null.");
			var type = args[0].GetType(compilation).OriginalDefinition;
			string customFormatterData = args[1].Expression.GetInnerText();

			dict.Add(type, Formatter.CreateFromString(compilation, customFormatterData));
		}

		return dict;
	}

	public static bool EqualsToTemplate<T>(this AttributeArgumentSyntax arg, T props) where T : IGeneratorProps =>
		SymbolEqualityComparer.Default.Equals(arg.GetType(props.Compilation), props.Template);
}
