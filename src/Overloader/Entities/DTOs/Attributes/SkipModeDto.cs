using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities.DTOs.Attributes;

public record SkipModeDto(
	bool ShouldBeSkipped,
	byte TemplateIndexFor,
	ITypeSymbol? TemplateTypeFor)
{
	public static SkipModeDto Parse(AttributeSyntax attribute, Compilation compilation)
	{
		var args = attribute.ArgumentList?.Arguments ?? [];
		if (args.Count < 1)
			throw new ArgumentException("Not enough arguments were specified")
				.WithLocation(attribute);

		if (args[0].Expression is not LiteralExpressionSyntax literalSyntax
		    || literalSyntax.Kind() is not (SyntaxKind.TrueLiteralExpression or SyntaxKind.FalseLiteralExpression))
			throw new ArgumentException("True or False should be specified")
				.WithLocation(attribute);
		
		bool shouldBeSkipped = literalSyntax.IsKind(SyntaxKind.TrueLiteralExpression);
		(byte templateIndexFor, var templateTypeFor) = attribute.ParseTemplateFor(compilation, 1);
		
		return new SkipModeDto(shouldBeSkipped, templateIndexFor, templateTypeFor);
	}
}
