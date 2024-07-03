using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities.Attributes;

public record SkipModeDto(
	bool ShouldBeSkipped,
	byte TemplateIndexFor,
	ITypeSymbol? TemplateTypeFor)
{
	public static SkipModeDto Parse(AttributeSyntax attribute, Compilation compilation)
	{
		var args = attribute.ArgumentList?.Arguments ?? new SeparatedSyntaxList<AttributeArgumentSyntax>();
		if (args.Count < 1)
			throw new ArgumentException("Not enough arguments were specified")
				.WithLocation(attribute);

		if (args[0].Expression is not LiteralExpressionSyntax literalSyntax
		    || literalSyntax.Kind() is not (SyntaxKind.TrueLiteralExpression or SyntaxKind.FalseLiteralExpression))
			throw new ArgumentException("True or False should be specified")
				.WithLocation(attribute);
		
		bool shouldBeSkipped = literalSyntax.IsKind(SyntaxKind.TrueLiteralExpression);
		
		const int templateForArgIndex = 1;
		byte templateIndexFor = 0;
		ITypeSymbol? templateTypeFor = null;
		
		switch (args.Count)
		{
			case templateForArgIndex:
				break;
			case templateForArgIndex + 1 when args[templateForArgIndex].NameColon is {Name.Identifier.ValueText: "templateTypeFor"}:
				templateTypeFor = args[templateForArgIndex].Expression.GetType(compilation);
				break;
			case templateForArgIndex + 2:
				templateIndexFor = byte.Parse(args[templateForArgIndex].GetText().ToString());
				templateTypeFor = args[templateForArgIndex + 1].Expression.GetType(compilation);
				break;
			default:
				throw new ArgumentException("Wrong count of arguments were specified")
					.WithLocation(attribute);
		}

		return new SkipModeDto(shouldBeSkipped, templateIndexFor, templateTypeFor);
	}
}
