using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Formatters;
using Overloader.Formatters.Params;

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
			var attrName = attribute.Name.ToString();
			if (attrName.Equals(AttributeNames.IntegrityAttr))
			{
				forceOverloadIntegrity = true;
				continue;
			}
			if (!attrName.Equals(AttributeNames.TAttr)) continue;
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
		_ => throw new ArgumentException("Unknown attribute.")
	};

	public static string GetInnerText(this ExpressionSyntax expressionSyntax) =>
		expressionSyntax.GetText().GetInnerText();

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
		       throw new ArgumentException("Type not found.");
	}

	public static Dictionary<ITypeSymbol, Formatter> GetFormatters(this IList<AttributeSyntax> attributeSyntaxes, Compilation compilation)
	{
		var dict = new Dictionary<ITypeSymbol, Formatter>(attributeSyntaxes.Count, SymbolEqualityComparer.Default);

		foreach (var formatterSyntax in attributeSyntaxes)
		{
			var args = formatterSyntax.ArgumentList?.Arguments ??
			           throw new ArgumentException("Argument list for formatter can't be null.");
			if (args.Count != 3) throw new ArgumentException("Not enough parameters for formatter.");

			var type = args[0].GetType(compilation);
			if (!type.IsDefinition) type = type.OriginalDefinition;

			var genericParams = ParseParams(((ArrayCreationExpressionSyntax) args[1].Expression).Initializer, compilation, false);
			var @params = ParseParams(((ArrayCreationExpressionSyntax) args[2].Expression).Initializer, compilation, true);

			dict.Add(type, new Formatter(genericParams, @params));
		}

		return dict;
	}

	private static IParam[] ParseParams(InitializerExpressionSyntax? initializer, Compilation compilation, bool withNames)
	{
		if (initializer is null) return Array.Empty<IParam>();
		if (withNames && initializer.Expressions.Count % 2 != 0)
			throw new ArgumentException($"Problem with count of expressions for named array in {initializer}.");

		var @params = new IParam[withNames ? initializer.Expressions.Count / 2 : initializer.Expressions.Count];
		string? name = null;

		for (int index = 0, paramIndex = 0; index < initializer.Expressions.Count; index++)
		{
			if (withNames)
			{
				if (initializer.Expressions[index] is not LiteralExpressionSyntax str) throw new ArgumentException();
				name = str.GetInnerText();
				index++;
			}

			@params[paramIndex++] = ParseParam(initializer.Expressions[index], compilation, name);
		}

		return @params;
	}

	private static IParam ParseParam(this ExpressionSyntax expressionSyntax, Compilation compilation, string? name)
	{
		switch (expressionSyntax)
		{
			case LiteralExpressionSyntax str when str.GetInnerText() == "T":
				return TemplateParam.Create(name);
			case TypeOfExpressionSyntax typeSyntax:
				return TypeParam.Create(typeSyntax.GetType(compilation), name);
			case ImplicitArrayCreationExpressionSyntax @switch:
				var expressions = @switch.Initializer.Expressions;
				if (expressions.Count == 0 || expressions.Count % 2 != 0) throw new ArgumentException();

				var switchDict = new Dictionary<ITypeSymbol, IParam>(expressions.Count / 2, SymbolEqualityComparer.Default);
				for (int switchParamIndex = 0; switchParamIndex < expressions.Count; switchParamIndex += 2)
				{
					var value = ParseParam(expressions[switchParamIndex + 1], compilation, name);
					if (value is SwitchParam)
						throw new ArgumentException(
							$"Switch statement in switch statement was detected in {expressionSyntax.ToString()}.");
					switchDict.Add(expressions[switchParamIndex].GetType(compilation), value);
				}

				return SwitchParam.Create(switchDict, name);
			default:
				throw new ArgumentException(
					$"Can't recognize syntax when try to parse parameter in {expressionSyntax.ToString()}.");
		}
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

	// TODO: check readonly ref
	public static string GetPreTypeValues(this TypeSyntax typeSyntax) =>
		typeSyntax is RefTypeSyntax ? "ref" : string.Empty;
}
