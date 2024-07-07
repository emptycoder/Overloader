using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities.DTOs.Attributes;

public record ModifierDto(
	string Modifier,
	string? InsteadOf,
	byte TemplateIndexFor,
	ITypeSymbol? TemplateTypeFor)
{
	public static ModifierDto Parse(AttributeSyntax attribute, Compilation compilation)
	{
		string modifier;
		string? insteadOf = null;
		byte templateIndexFor = 0;
		ITypeSymbol? templateTypeFor = null;
		
		var args = attribute.ArgumentList?.Arguments ?? [];
		switch (args.Count)
		{
			case 1:
				if (args[0].Expression is not LiteralExpressionSyntax modifierExpression
				    || !modifierExpression.IsKind(SyntaxKind.StringLiteralExpression))
					throw new ArgumentException("Allowed only string literal.")
						.WithLocation(args[0].Expression);
				
				modifier = modifierExpression.GetVariableName();
				break;
			case 2 when args[1].NameColon is {Name.Identifier.ValueText: "templateTypeFor"}:
				templateTypeFor = args[1].Expression.GetType(compilation);
				goto case 1;
			case 2:
				if (args[1].Expression is not LiteralExpressionSyntax insteadOfExpression)
					throw new ArgumentException("Allowed only string or null literals.")
						.WithLocation(args[0].Expression);

				insteadOf = insteadOfExpression.Kind() switch
				{
					SyntaxKind.StringLiteralExpression => insteadOfExpression.GetVariableName(),
					SyntaxKind.NullLiteralExpression => null,
					_ => throw new ArgumentException($"Literal ({insteadOfExpression}) not allowed")
						.WithLocation(insteadOfExpression)
				};
				goto case 1;
			case 3 when args[2].NameColon is {Name.Identifier.ValueText: "templateTypeFor"}:
				templateTypeFor = args[2].Expression.GetType(compilation);
				goto case 2;
			case 4:
				templateIndexFor = byte.Parse(args[2].GetText().ToString());
				templateTypeFor = args[3].Expression.GetType(compilation);
				goto case 2;
			default:
				throw new ArgumentException("Wrong count of arguments were specified")
					.WithLocation(attribute);
		}
		
		return new ModifierDto(modifier, insteadOf, templateIndexFor, templateTypeFor);
	}
}
