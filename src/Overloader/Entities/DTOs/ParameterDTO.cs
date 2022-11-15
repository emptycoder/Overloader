using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities.DTOs;

public struct ParameterDto
{
	public AttributeSyntax Attribute;
	public bool ForceOverloadIntegrity;
	public string? CombineWith;
	public List<(string Modifier, string? InsteadOf, ITypeSymbol? FormatterType)> ModifierChangers;

	public static bool TryGetParameterDtoByTemplate(ParameterSyntax syntaxNode,
		IGeneratorProps props,
		out ParameterDto tAttrDto)
	{
		tAttrDto = new ParameterDto
		{
			ModifierChangers = new List<(string Modifier, string? InsteadOf, ITypeSymbol? FormatterType)>(0)
		};
		foreach (var attrList in syntaxNode.AttributeLists)
		foreach (var attribute in attrList.Attributes)
		{
			string attrName = attribute.Name.GetName();
			switch (attrName)
			{
				case Constants.IntegrityAttr:
					tAttrDto.ForceOverloadIntegrity = true;
					continue;
				case Constants.CombineWithAttr when attribute.ArgumentList is {Arguments: var args}:
					if (args.Count != 1)
						throw new ArgumentException("Not allowed with arguments count != 1.")
							.WithLocation(syntaxNode);
					tAttrDto.CombineWith = args[0].Expression.GetVariableName();
					continue;
				case Constants.TAttr:
					if (attribute.ArgumentList is {Arguments.Count: > 1} &&
					    attribute.ArgumentList.Arguments[1].EqualsToTemplate(props)) continue;
					tAttrDto.Attribute = attribute;
					continue;
				case Constants.ParamModifierAttr when attribute.ArgumentList is {Arguments: var args}:
					int argsCount = args.Count;
					if (args[0].Expression is not LiteralExpressionSyntax modifierExpression
					    || !modifierExpression.IsKind(SyntaxKind.StringLiteralExpression))
						throw new ArgumentException("Allowed only string literal.")
							.WithLocation(args[0].Expression);

					if (argsCount == 1)
						tAttrDto.ModifierChangers.Add((modifierExpression.GetVariableName(), null, null));

					if (args[1].Expression is not LiteralExpressionSyntax insteadOfExpression)
						throw new ArgumentException("Allowed only string or null literals.")
							.WithLocation(args[0].Expression);

					string? insteadOf = insteadOfExpression.Kind() switch
					{
						SyntaxKind.StringLiteralExpression => insteadOfExpression.GetVariableName(),
						SyntaxKind.NullLiteralExpression => null,
						_ => throw new ArgumentException($"Literal ({insteadOfExpression}) not allowed")
							.WithLocation(insteadOfExpression)
					};

					if (argsCount == 2)
						tAttrDto.ModifierChangers.Add((
							modifierExpression.GetVariableName(),
							insteadOf,
							null));

					var type = args[2].Expression.GetType(props.Compilation);
					var namedTypeSymbol = type.GetClearType();
					if (namedTypeSymbol.IsUnboundGenericType) type = type.OriginalDefinition;

					tAttrDto.ModifierChangers.Add((
						modifierExpression.GetVariableName(),
						insteadOf,
						type));
					continue;
			}
		}

		return tAttrDto.Attribute != null;
	}
}
