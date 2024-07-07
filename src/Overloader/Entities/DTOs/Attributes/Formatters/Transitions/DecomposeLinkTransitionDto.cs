using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities.DTOs.Attributes.Formatters.Transitions;

public sealed record DecomposeLinkTransitionDto(
	ITypeSymbol TemplateType,
	Dictionary<string, string> ParamsMap)
{
	public static DecomposeLinkTransitionDto Parse(
		ExpressionSyntax type,
		ExpressionSyntax mapParams,
		Compilation compilation)
	{
		if (type is not TypeOfExpressionSyntax)
			throw new ArgumentException($"{nameof(type)} should be {nameof(TypeOfExpressionSyntax)}.")
				.WithLocation(type);

		if (mapParams is not ArrayCreationExpressionSyntax {Initializer.Expressions: var expressions})
			throw new ArgumentException($"Expression isn't {nameof(ArrayCreationExpressionSyntax)}.")
				.WithLocation(mapParams);

		if (expressions.Count == 0 || expressions.Count % 2 != 0)
			throw new ArgumentException("Not key/value. Map for expressions must contains only even count of links.")
				.WithLocation(mapParams);

		var paramsMap = new Dictionary<string, string>(expressions.Count / 2);
		for (int index = 0; index < expressions.Count; index++)
			paramsMap.Add(expressions[index++].GetVariableName(), expressions[index].GetVariableName());

		return new DecomposeLinkTransitionDto(type.GetType(compilation), paramsMap);
	}
}
