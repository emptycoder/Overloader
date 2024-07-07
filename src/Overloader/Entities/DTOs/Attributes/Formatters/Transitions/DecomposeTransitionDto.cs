using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Exceptions;

namespace Overloader.Entities.DTOs.Attributes.Formatters.Transitions;

public sealed record DecomposeTransitionDto(
	DecomposeLinkTransitionDto[] Links)
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

	public static DecomposeTransitionDto Parse(in SeparatedSyntaxList<ExpressionSyntax> expressions, Compilation compilation)
	{
		try
		{
			int argsCount = expressions.Count - 1;
			if (argsCount % 2 != 0)
				throw new ArgumentException("Not [type]/[map params].")
					.WithLocation(expressions[1]);

			var transitionLinks = new DecomposeLinkTransitionDto[argsCount / 2];
			for (int expressionIndex = 1, transitionIndex = 0; transitionIndex < transitionLinks.Length; transitionIndex++)
				transitionLinks[transitionIndex] = DecomposeLinkTransitionDto.Parse(
					expressions[expressionIndex++],
					expressions[expressionIndex++],
					compilation);

			return new DecomposeTransitionDto(transitionLinks);
		}
		catch (Exception ex)
		{
			if (ex is LocationException || !expressions.Any()) throw;
			throw ex.WithLocation(expressions[0]);
		}
	}
}
