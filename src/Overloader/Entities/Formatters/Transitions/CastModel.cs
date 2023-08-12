using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities.Formatters.Transitions;

public sealed record CastModel(
	ParamModel[] Types,
	string CastCodeTemplate)
{
	public static CastModel Parse(in SeparatedSyntaxList<ExpressionSyntax> expressions, Compilation compilation)
	{
		try
		{
			int argsCount = expressions.Count - 1;
			var templates = new ParamModel[argsCount - 1];
			for (int index = 0, argIndex = 1; index < argsCount - 1; index++, argIndex++)
			{
				var modifierStr = string.Empty;
				if (expressions[argIndex] is MemberAccessExpressionSyntax modifierExpression)
				{
					modifierStr = modifierExpression.Name.ToString() switch
					{
						nameof(ParamModifier.In) => "in",
						nameof(ParamModifier.Ref) => "ref",
						nameof(ParamModifier.Out) => "out",
						nameof(ParamModifier.None) => string.Empty,
						_ => throw new ArgumentException($"Can't parse {nameof(Modifier)}: {modifierExpression.Name}.")
							.WithLocation(expressions[argIndex])
					};
					argIndex++;
				}

				if (expressions[argIndex] is not TypeOfExpressionSyntax)
					throw new ArgumentException($"Type '{argIndex}' should be {nameof(TypeOfExpressionSyntax)}.")
						.WithLocation(expressions[argIndex]);
				
				var type = expressions[argIndex].GetType(compilation);
				templates[index] = new ParamModel
				{
					Name = expressions[++argIndex].GetStringValue(compilation),
					Modifier = modifierStr,
					Type = type,
					IsUnboundType = type.GetClearType().IsUnboundGenericType
				};
			}

			return new CastModel(
				templates,
				expressions[expressions.Count - 1].GetStringValue(compilation));
		}
		catch (Exception ex)
		{
			if (ex is LocationException || !expressions.Any()) throw;
			throw ex.WithLocation(expressions[0]);
		}
	}
}
