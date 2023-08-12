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
			var templates = new List<ParamModel>();
			int argIndex = 1;
			for (; argIndex < expressions.Count - 2; argIndex++)
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
					throw new ArgumentException($"Type '{expressions[argIndex].ToString()}' should be {nameof(TypeOfExpressionSyntax)}.")
						.WithLocation(expressions[argIndex]);
				
				var type = expressions[argIndex].GetType(compilation);
				templates.Add(new ParamModel
				{
					Name = expressions[++argIndex].GetStringValue(compilation),
					Modifier = modifierStr,
					Type = type,
					IsUnboundType = type.GetClearType().IsUnboundGenericType
				});
			}

			if (argIndex != expressions.Count - 1)
				throw new ArgumentException($"Structure of {nameof(CastModel)} was broken.");

			return new CastModel(
				templates.ToArray(),
				expressions[argIndex].GetStringValue(compilation));
		}
		catch (Exception ex)
		{
			if (ex is LocationException || !expressions.Any()) throw;
			throw ex.WithLocation(expressions[0]);
		}
	}
}
