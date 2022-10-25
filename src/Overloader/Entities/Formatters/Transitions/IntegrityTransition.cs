using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities.Formatters.Transitions;

internal sealed record IntegrityTransition(
	ITypeSymbol TemplateType,
	string IntegrityCastCodeTemplate)
{
	public static IntegrityTransition Parse(in SeparatedSyntaxList<ExpressionSyntax> expressions, Compilation compilation)
	{
		if (expressions.Count != 2)
			throw new ArgumentException("Not [type]/[cast code template].")
				.WithLocation(expressions[0]);

		if (expressions[0] is not TypeOfExpressionSyntax)
			throw new ArgumentException($"Type must be {nameof(TypeOfExpressionSyntax)}.")
				.WithLocation(expressions[0]);

		if (expressions[1] is not LiteralExpressionSyntax literal || literal.Kind() != SyntaxKind.StringLiteralExpression)
			throw new ArgumentException("Cast code template isn't StringLiteralExpressionSyntax.")
				.WithLocation(expressions[1]);

		return new IntegrityTransition(
			expressions[0].GetType(compilation),
			expressions[1].GetVariableName());
	}
}
