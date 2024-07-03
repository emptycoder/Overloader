using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities.Attributes;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities.DTOs;

public struct ParameterDto
{
	public TAttributeDto? Attribute;
	public bool HasForceOverloadIntegrity;
	public string? CombineWith;
	public List<(string Modifier, string? InsteadOf, ITypeSymbol? FormatterType)> ModifierChangers;

	public static bool TryGetParameterDtoByTemplate(ParameterSyntax syntaxNode,
		IGeneratorProps props,
		out ParameterDto parameterDto)
	{
		parameterDto = new ParameterDto
		{
			ModifierChangers = new List<(string Modifier, string? InsteadOf, ITypeSymbol? FormatterType)>(0)
		};
		foreach (var attrList in syntaxNode.AttributeLists)
		foreach (var attribute in attrList.Attributes)
		{
			string attrName = attribute.Name.GetName();
			switch (attrName)
			{
				case Integrity.TagName:
					parameterDto.HasForceOverloadIntegrity = true;
					continue;
				case nameof(Overloader.CombineWith) when attribute.ArgumentList is {Arguments: var args}:
					if (args.Count != 1)
						throw new ArgumentException("Not allowed with arguments count != 1.")
							.WithLocation(syntaxNode);
					parameterDto.CombineWith = args[0].Expression.GetVariableName();
					continue;
				case TAttribute.TagName:
					var tAttrDto = TAttributeDto.Parse(attribute, props.Compilation);
					if (SymbolEqualityComparer.Default.Equals(tAttrDto.ForType, props.Templates[tAttrDto.TemplateIndex])
					    || tAttrDto.ForType is null)
					{
						parameterDto.Attribute = tAttrDto;
					}
					continue;
				case Modifier.TagName when attribute.ArgumentList is {Arguments: var args}:
					int argsCount = args.Count;
					if (args[0].Expression is not LiteralExpressionSyntax modifierExpression
					    || !modifierExpression.IsKind(SyntaxKind.StringLiteralExpression))
						throw new ArgumentException("Allowed only string literal.")
							.WithLocation(args[0].Expression);

					if (argsCount == 1)
						parameterDto.ModifierChangers.Add((modifierExpression.GetVariableName(), null, null));

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
						parameterDto.ModifierChangers.Add((
							modifierExpression.GetVariableName(),
							insteadOf,
							null));

					var type = args[2].Expression.GetType(props.Compilation);
					var namedTypeSymbol = type.GetClearType();
					if (namedTypeSymbol.IsUnboundGenericType) type = type.OriginalDefinition;

					parameterDto.ModifierChangers.Add((
						modifierExpression.GetVariableName(),
						insteadOf,
						type));
					continue;
			}
		}

		return parameterDto.Attribute != null;
	}
}
