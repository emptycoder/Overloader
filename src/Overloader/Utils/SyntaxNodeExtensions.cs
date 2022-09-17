﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Entities.Formatters;
using Overloader.Exceptions;

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

	public static bool TryGetTAttrByTemplate(this ParameterSyntax syntaxNode,
		IGeneratorProps props,
		out AttributeSyntax? attr,
		out bool forceOverloadIntegrity)
	{
		forceOverloadIntegrity = false;
		attr = default;
		foreach (var attrList in syntaxNode.AttributeLists)
		foreach (var attribute in attrList.Attributes)
		{
			string attrName = attribute.Name.GetName();
			if (attrName.Equals(Attributes.IntegrityAttr))
			{
				forceOverloadIntegrity = true;
				continue;
			}

			if (!attrName.Equals(Attributes.TAttr)) continue;
			if (attribute.ArgumentList is {Arguments.Count: > 1} &&
			    (props.Template is null || attribute.ArgumentList.Arguments[1].EqualsToTemplate(props))) continue;

			attr = attribute;
		}

		return attr != null;
	}

	public static string GetName(this NameSyntax nameSyntax) => nameSyntax switch
	{
		// [name]
		IdentifierNameSyntax identifierSyntax => identifierSyntax.Identifier.ValueText,
		// [namespace.name]
		QualifiedNameSyntax qualifiedNameSyntax => qualifiedNameSyntax.Right.Identifier.ValueText,
		_ => throw new ArgumentException("Unknown attribute.").WithLocation(nameSyntax.GetLocation())
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

	public static ITypeSymbol GetType(this CSharpSyntaxNode node, Compilation compilation)
	{
		if (node is TypeOfExpressionSyntax typeOfExpressionSyntax)
			node = typeOfExpressionSyntax.Type;

		if (node is RefTypeSyntax refTypeSyntax)
			node = refTypeSyntax.Type;

		var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
		return semanticModel.GetTypeInfo(node).Type ??
		       throw new ArgumentException("Type not found.").WithLocation(node.GetLocation());
	}

	public static Dictionary<ITypeSymbol, Formatter> GetFormatters(this IList<AttributeSyntax> attributeSyntaxes, Compilation compilation)
	{
		var dict = new Dictionary<ITypeSymbol, Formatter>(attributeSyntaxes.Count, SymbolEqualityComparer.Default);
		foreach (var formatterSyntax in attributeSyntaxes)
		{
			var result = Formatter.ParseFormatter(formatterSyntax, compilation);
			dict.Add(result.Type, result.Formatter);
		}

		return dict;
	}

	public static bool EqualsToTemplate<T>(this AttributeArgumentSyntax arg, T props) where T : IGeneratorProps =>
		SymbolEqualityComparer.Default.Equals(arg.GetType(props.Compilation), props.Template);

	public static ITypeSymbol GetMemberType(this ITypeSymbol type, string name)
	{
		var member = type.GetMembers(name).FirstOrDefault() ??
		             throw new ArgumentException($"Member name ({name}) wasn't found in parameter types.");
		return member switch
		{
			IFieldSymbol fieldSymbol => fieldSymbol.Type,
			IPropertySymbol propertySymbol => propertySymbol.Type,
			_ => throw new ArgumentException($"Member with name '{name}' isn't property or field.")
		};
	}

	public static string GetPreTypeValues(this TypeSyntax typeSyntax) =>
		typeSyntax is RefTypeSyntax refSyntax
			? $"{refSyntax.RefKeyword.ToFullString()}{refSyntax.ReadOnlyKeyword.ToFullString()}"
			: string.Empty;
}
