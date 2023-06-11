using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Formatters.Transitions;

public sealed record IntegrityTransition(
	ITypeSymbol TemplateType,
	string IntegrityCastCodeTemplate)
{
	public static IntegrityTransition Parse(in SeparatedSyntaxList<ExpressionSyntax> expressions, Compilation compilation)
	{
		if (expressions.Count is not 2)
			throw new ArgumentException("Not [type]/[cast code template].")
				.WithLocation(expressions[0]);

		if (expressions[0] is not TypeOfExpressionSyntax)
			throw new ArgumentException($"{nameof(TemplateType)} type must be {nameof(TypeOfExpressionSyntax)}.")
				.WithLocation(expressions[0]);

		string castInBlockTemplate;
		switch (expressions[1])
		{
			case LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression):
				castInBlockTemplate = literal.GetVariableName();
				break;
			case MemberAccessExpressionSyntax:
			case InterpolatedStringExpressionSyntax:
				var value = compilation.GetSemanticModel(expressions[1].SyntaxTree).GetConstantValue(expressions[1]);
				if (!value.HasValue || value.Value is not string) goto default;
				castInBlockTemplate = (string) value.Value!;
				break;
			default:
				throw new ArgumentException($"{nameof(IntegrityCastCodeTemplate)} isn't StringLiteral/InterpolationString/MemberAccess.")
					.WithLocation(expressions[1]);
		}
		
		return new IntegrityTransition(
			expressions[0].GetType(compilation),
			castInBlockTemplate);
	}
}
