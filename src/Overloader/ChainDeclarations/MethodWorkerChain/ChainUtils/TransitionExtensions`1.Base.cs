using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ContentBuilders;
using Overloader.Enums;
using Overloader.Models;

namespace Overloader.ChainDeclarations.MethodWorkerChain.ChainUtils;

internal static partial class TransitionExtensions
{
	public delegate void TransitionWriter(
		SourceBuilder headerBuilder,
		SourceBuilder bodyBuilder,
		GeneratorProperties props,
		ParameterData mappedParam,
		ParameterSyntax parameter,
		Span<int> transitionIndexes,
		ref int paramIndex);

	public static void WriteTransitionOverload(
		this SourceBuilder headerBuilder,
		TransitionWriter transitionWriter,
		SourceBuilder bodyBuilder,
		GeneratorProperties props,
		in SeparatedSyntaxList<ParameterSyntax> parameters,
		Span<int> transitionIndexes,
		bool combineWithMode = false)
	{
		for (int index = 0, paramIndex = 0;;)
		{
			var mappedParam = props.Store.OverloadMap![index];
			var parameter = parameters[index];

			if (!combineWithMode || mappedParam.IsCombineNotExists)
			{
				transitionWriter(headerBuilder, bodyBuilder, props, mappedParam, parameter, transitionIndexes, ref paramIndex);

				if (++index == parameters.Count) break;
				if (!combineWithMode || props.Store.OverloadMap[index].IsCombineNotExists)
					headerBuilder.AppendWoTrim(", ");
				bodyBuilder.AppendWoTrim(", ");
			}
			else
			{
				int tempParamIndex = 0;
				for (int combinedIndex = 0; combinedIndex < mappedParam.CombineIndex; combinedIndex++)
				{
					var param = props.Store.OverloadMap[combinedIndex];
					if (param.ParameterAction == ParameterAction.FormatterReplacement
					    && !param.IsCombineNotExists) tempParamIndex++;
				}

				transitionWriter(EmptySourceBuilder.Instance,
					bodyBuilder,
					props,
					props.Store.OverloadMap![mappedParam.CombineIndex],
					parameters[mappedParam.CombineIndex],
					transitionIndexes,
					ref tempParamIndex);

				if (++index == parameters.Count) break;
				bodyBuilder.AppendWoTrim(", ");
			}
		}
	}

	public static void AppendCombined(this SourceBuilder bodyBuilder,
		GeneratorProperties props,
		ParameterData mappedParam,
		ParameterSyntax parameter)
	{
		switch (mappedParam.ParameterAction)
		{
			case ParameterAction.FormatterReplacement:
			{
				bodyBuilder.AppendWoTrim(EmptySourceBuilder.Instance
					.AppendFormatterParam(props,
						props.Store.OverloadMap![mappedParam.CombineIndex].Type,
						parameter.Identifier.ValueText));
				break;
			}
			default:
				bodyBuilder.AppendCombinedSimple(mappedParam, parameter);
				break;
		}
	}

	public static void AppendCombinedSimple(this SourceBuilder bodyBuilder, ParameterData mappedParam, ParameterSyntax parameter)
	{
		if (mappedParam.Type.IsRefLikeType)
			bodyBuilder.AppendWoTrim("ref ");
		bodyBuilder.AppendWoTrim(parameter.Identifier.ValueText);
	}

	public static void AppendVariableToBody(this SourceBuilder bodyBuilder, ParameterSyntax parameter, string? paramName = null)
	{
		if (parameter.Modifiers.Any(SyntaxKind.RefKeyword))
			bodyBuilder.AppendWoTrim("ref ");
		bodyBuilder.AppendWoTrim(paramName ?? parameter.Identifier.ValueText);
	}
}
