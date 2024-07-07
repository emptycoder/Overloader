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
			case >= 2:
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
			default:
				throw new ArgumentException("Wrong count of arguments were specified")
					.WithLocation(attribute);
		}
		
		(byte templateIndexFor, var templateTypeFor) = attribute.ParseTemplateFor(compilation, 2);
		
		return new ModifierDto(modifier, insteadOf, templateIndexFor, templateTypeFor);
	}
}
