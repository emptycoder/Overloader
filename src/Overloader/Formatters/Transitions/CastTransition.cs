using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Formatters.Transitions;

public sealed record CastTransition(
	(ITypeSymbol Type, bool IsUnboundTemplateGenericType)[] Templates,
	string IntegrityCastCodeTemplate)
{
	public static CastTransition Parse(in SeparatedSyntaxList<ExpressionSyntax> expressions, Compilation compilation)
	{
		if (expressions.Count < 2)
			throw new ArgumentException("Not Array<[type]>/[cast code template].")
				.WithLocation(expressions[0]);
		
		var templates = new (ITypeSymbol, bool)[expressions.Count - 1];
		for (int index = 0; index < expressions.Count - 1; index++)
		{
			if (expressions[index] is not TypeOfExpressionSyntax)
				throw new ArgumentException($"{nameof(Templates)} type '{index}' must be {nameof(TypeOfExpressionSyntax)}.")
					.WithLocation(expressions[index]);
			
			var type = expressions[index].GetType(compilation);
			templates[index] = (type, type.GetClearType().IsUnboundGenericType);
		}

		string castInBlockTemplate;
		switch (expressions[templates.Length])
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

		
		return new CastTransition(
			templates,
			castInBlockTemplate);
	}
}
