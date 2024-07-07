using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities.DTOs.Attributes;

public record TSpecifyDto(
	TypeSyntax[] DefaultTypeSyntaxes,
	string[]? FormattersToUse)
{
	public static TSpecifyDto Parse(AttributeSyntax attribute)
	{
		const int headerInfoIndexEnd = 1;
		if (attribute.ArgumentList is not {Arguments: var args}
		    || args.Count < headerInfoIndexEnd)
			throw new ArgumentException($"Count of arguments must greater or equals to {headerInfoIndexEnd}.")
				.WithLocation(attribute);

		TypeSyntax[] templateTypes;
		switch (args[0].Expression)
		{
			case ImplicitArrayCreationExpressionSyntax {Initializer.Expressions: {Count: >= 1} expressions}:
				templateTypes = new TypeSyntax[expressions.Count];
				for (int index = 0; index < expressions.Count; index++)
				{
					if (expressions[index] is not TypeOfExpressionSyntax typeSyntax)
						throw new ArgumentException($"Expression isn't {nameof(TypeOfExpressionSyntax)}.")
							.WithLocation(attribute);

					templateTypes[index] = typeSyntax.Type;
				}
				break;
			case TypeOfExpressionSyntax typeOfSyntax:
				templateTypes = [typeOfSyntax.Type];
				break;
			case CollectionExpressionSyntax { Elements: { Count: >= 1 } collectionExpressions }:
				templateTypes = new TypeSyntax[collectionExpressions.Count];
				for (int index = 0; index < collectionExpressions.Count; index++)
				{
					if (collectionExpressions[index] is not ExpressionElementSyntax { Expression: TypeOfExpressionSyntax typeSyntax })
						throw new ArgumentException($"Expression isn't {nameof(TypeOfExpressionSyntax)}.")
							.WithLocation(attribute);
				
					templateTypes[index] = typeSyntax.Type;
				}
				break;
			default:
				throw new ArgumentException("Template types isn't specified for overload.")
					.WithLocation(attribute);
		}

		int count = args.Count - headerInfoIndexEnd;
		var formattersToUse = count == 0 ? [] : new string[count];
		for (int argIndex = headerInfoIndexEnd, formatterIndex = 0; argIndex < args.Count; argIndex++, formatterIndex++)
		{
			if (args[argIndex].Expression is not LiteralExpressionSyntax literalSyntax)
				throw new ArgumentException($"Formatter identifier should be {nameof(LiteralExpressionSyntax)}.")
					.WithLocation(args[argIndex].Expression);

			formattersToUse[formatterIndex] = literalSyntax.GetInnerText();
		}

		return new TSpecifyDto(templateTypes, formattersToUse);
	}
}
