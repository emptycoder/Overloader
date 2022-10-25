using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities.Formatters.Transitions;

internal sealed record DeconstructTransition(DeconstructTransitionLink[] Links)
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

	public static DeconstructTransition Parse(in SeparatedSyntaxList<ExpressionSyntax> expressions, Compilation compilation)
	{
		if (expressions.Count % 2 != 0)
			throw new ArgumentException("Not [type]/[map params].")
				.WithLocation(expressions[0]);

		var transitionLinks = new DeconstructTransitionLink[expressions.Count / 2];
		for (int index = 0; index < transitionLinks.Length; index++)
			transitionLinks[index] = DeconstructTransitionLink.Parse(
				expressions[index++],
				expressions[index],
				compilation);

		return new DeconstructTransition(transitionLinks);
	}
}

internal sealed record DeconstructTransitionLink(
	ITypeSymbol TemplateType,
	Dictionary<string, string> ParamsMap)
{
	public static DeconstructTransitionLink Parse(
		ExpressionSyntax type,
		ExpressionSyntax mapParams,
		Compilation compilation)
	{
		if (type is not TypeOfExpressionSyntax)
			throw new ArgumentException($"{nameof(type)} must be {nameof(TypeOfExpressionSyntax)}.")
				.WithLocation(type);

		if (mapParams is not ArrayCreationExpressionSyntax {Initializer.Expressions: var expressions})
			throw new ArgumentException($"Expression isn't {nameof(ArrayCreationExpressionSyntax)} expression.")
				.WithLocation(mapParams);
		
		if (expressions.Count % 2 != 0)
			throw new ArgumentException("Not key/value. Map for expressions must contains only even count of links.")
				.WithLocation(mapParams);

		var paramsMap = new Dictionary<string, string>(expressions.Count / 2);
		for (int index = 0; index < expressions.Count; index++)
			paramsMap.Add(expressions[index++].GetVariableName(), expressions[index].GetVariableName());

		return new DeconstructTransitionLink(type.GetType(compilation), paramsMap);
	}
}
