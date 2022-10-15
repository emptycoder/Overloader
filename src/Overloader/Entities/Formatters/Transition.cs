using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities.Formatters;

internal sealed record Transition(TransitionLink[] Links)
{
	public bool TryToFindReplacement(string paramName, out string? result, out int transitionIndex)
	{
		for (transitionIndex = 0; transitionIndex < Links.Length; transitionIndex++)
		{
			if (Links[transitionIndex].ParamsMap.TryGetValue(paramName, out result))
				return true;
		}

		result = default;
		return false;
	}

	public static Transition Parse(ExpressionSyntax expression, Compilation compilation)
	{
		if (expression is not ArrayCreationExpressionSyntax {Initializer.Expressions: var expressions})
			throw new ArgumentException(
					$"Arg of {nameof(Formatter)} must be {nameof(ArrayCreationExpressionSyntax)}.")
				.WithLocation(expression);

		if (expressions.Count % 2 != 0)
			throw new ArgumentException("Not key/value.")
				.WithLocation(expression);

		var transitionLinks = new TransitionLink[expressions.Count / 2];
		for (int index = 0; index < transitionLinks.Length; index++)
			transitionLinks[index] = TransitionLink.Parse(
				expressions[index++],
				expressions[index],
				compilation);

		return new Transition(transitionLinks);
	}
}

internal sealed record TransitionLink(ITypeSymbol TemplateType, Dictionary<string, string> ParamsMap)
{
	public static TransitionLink Parse(ExpressionSyntax keyExpression, ExpressionSyntax valueExpression, Compilation compilation)
	{
		if (keyExpression is not TypeOfExpressionSyntax typeExpression)
			throw new ArgumentException($"{nameof(keyExpression)} must be {nameof(TypeOfExpressionSyntax)}.")
				.WithLocation(keyExpression);

		if (valueExpression is not ArrayCreationExpressionSyntax {Initializer.Expressions: var expressions})
			throw new ArgumentException($"Expression isn't {nameof(ArrayCreationExpressionSyntax)} expression.")
				.WithLocation(valueExpression);
		if (expressions.Count % 2 != 0)
			throw new ArgumentException("Not key/value. Map for expressions must contains only even count of links.")
				.WithLocation(valueExpression);

		var paramsMap = new Dictionary<string, string>(expressions.Count / 2);
		for (int index = 0; index < expressions.Count; index++)
			paramsMap.Add(expressions[index++].GetVariableName(), expressions[index].GetVariableName());

		return new TransitionLink(typeExpression.Type.GetType(compilation), paramsMap);
	}
}
