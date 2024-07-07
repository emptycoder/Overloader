using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities.DTOs.Attributes;

public record TOverloadDto(
	string OverloadClassName,
	TypeSyntax[] TypeSyntaxes,
	string[] FormattersToUse)
{
	public static TOverloadDto Parse(string className, AttributeSyntax attribute)
	{
		var args = attribute.ArgumentList?.Arguments ?? [];
		string[]? formattersToUse = null;
		switch (args.Count)
		{
			case 1:
				break;
			case 2 when args[1].NameColon is {Name.Identifier.ValueText: "formatters"}:
				formattersToUse = new string[args.Count - 1];
				for (int argIndex = 1, index = 0; argIndex < args.Count; argIndex++, index++)
					formattersToUse[index] = args[argIndex].Expression.GetInnerText();
				break;
			case 2:
				throw new ArgumentException($"Need to present regex replacement parameter for {TOverload.TagName}.")
					.WithLocation(attribute);
			case >= 3:
				switch (args[1].Expression)
				{
					case LiteralExpressionSyntax {RawKind: (int) SyntaxKind.NullLiteralExpression} when
						args[1].Expression is LiteralExpressionSyntax {RawKind: (int) SyntaxKind.NullLiteralExpression}:
						break;
					case LiteralExpressionSyntax {RawKind: (int) SyntaxKind.StringLiteralExpression} when
						args[1].Expression is LiteralExpressionSyntax {RawKind: (int) SyntaxKind.StringLiteralExpression}:
						className = Regex.Replace(className,
							args[1].Expression.GetInnerText(),
							args[2].Expression.GetInnerText());
						break;
					default:
						throw new ArgumentException($"Argument should be null or {nameof(SyntaxKind.StringLiteralExpression)}.")
							.WithLocation(args[1].Expression);
				}

				formattersToUse = new string[args.Count - 3];
				for (int argIndex = 3, index = 0; argIndex < args.Count; argIndex++, index++)
					formattersToUse[index] = args[argIndex].Expression.GetInnerText();
				break;
			default:
				throw new ArgumentException($"Unexpected count of args for {TOverload.TagName}.")
					.WithLocation(attribute);
		}

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

		return new TOverloadDto(className, templateTypes, formattersToUse ?? []);
	}
}
