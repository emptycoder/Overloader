using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Overloader.Utils;

public static class SyntaxNodeExtensions
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

	public static bool TryGetAttribute(this ParameterSyntax syntaxNode, string attrName, out AttributeSyntax? attr)
	{
		foreach (var attrList in syntaxNode.AttributeLists)
		foreach (var attribute in attrList.Attributes)
		{
			if (!attribute.Name.ToString().Equals(attrName)) continue;

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

	public static string GetInnerText(this SourceText sourceText) =>
		sourceText.GetSubText(new TextSpan(1, sourceText.Length - 2)).ToString();

	public static bool ContainsAttrInParams(this MethodDeclarationSyntax method, params string[] attrName)
	{
		foreach (var parameter in method.ParameterList.Parameters)
		foreach (var attributeList in parameter.AttributeLists)
		foreach (var attribute in attributeList.Attributes)
			if (attrName.Contains(attribute.Name.ToString())) return true;

		return false;
	}

	public static SyntaxNode GetTopParent(this SyntaxNode syntax)
	{
		var topNode = syntax;
		while (topNode.Parent is not null)
			topNode = topNode.Parent;

		return topNode;
	}
}
