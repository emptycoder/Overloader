using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities.Formatters;

internal sealed record Transition(TransitionLink[] Links)
{
	public string FindReplacement(string paramName, out int transitionIndex)
	{
		for (transitionIndex = 0; transitionIndex < Links.Length; transitionIndex++)
		{
			if (Links[transitionIndex].ParamsMap.TryGetValue(paramName, out var result))
				return result;
		}

		throw new ArgumentException($"Not found replacement for {paramName}.");
	}
	
	public static Transition Parse(InitializerExpressionSyntax initializer, Compilation compilation)
	{
		if (initializer.Expressions.Count != 0) throw new ArgumentException(
			$"Empty {nameof(Transition)} not allowed!").WithLocation(initializer);

		var transitionLinks = new TransitionLink[initializer.Expressions.Count];
		for (int index = 0; index < transitionLinks.Length; index++)
			transitionLinks[index] = TransitionLink.Parse(initializer.Expressions[index], compilation);

		return new Transition(transitionLinks);
	}
}

internal sealed record TransitionLink(ITypeSymbol Type, Dictionary<string, string> ParamsMap)
{
	public static TransitionLink Parse(ExpressionSyntax expression, Compilation compilation)
	{
		if (expression is not InitializerExpressionSyntax { Expressions: var transitionExpressions })
			throw new ArgumentException($"Expression isn't {nameof(InitializerExpressionSyntax)} expression.")
				.WithLocation(expression);
		if (transitionExpressions.Count != 2)
			throw new ArgumentException($"Not key/value. Count of expressions ({transitionExpressions.Count}) not equals to 2.")
				.WithLocation(expression);
		if (transitionExpressions[1] is not InitializerExpressionSyntax { Expressions: var mapExpressions })
			throw new ArgumentException($"Expression isn't {nameof(InitializerExpressionSyntax)} expression.")
				.WithLocation(expression);
		if (mapExpressions.Count % 2 != 0)
			throw new ArgumentException("Not key/value. Map for expressions must contains only even count of links.")
				.WithLocation(expression);

		var paramsMap = new Dictionary<string, string>(mapExpressions.Count / 2);
		for (int index = 0; index < mapExpressions.Count; index++)
			paramsMap.Add(mapExpressions[index++].GetVariableName(), mapExpressions[index].GetVariableName());

		return new TransitionLink(transitionExpressions[0].GetType(compilation), paramsMap);
	}
}
