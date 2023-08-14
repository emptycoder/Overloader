using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ContentBuilders;
using Overloader.Entities;
using Overloader.Entities.Formatters;
using Overloader.Enums;

namespace Overloader.ChainDeclarations.Overloads.Utils;

public static partial class TransitionExtensions
{
	public delegate void TransitionWriter(
		SourceBuilder headerBuilder,
		SourceBuilder bodyBuilder,
		XmlDocumentation xmlDocumentation,
		GeneratorProperties props,
		ParameterData mappedParam,
		ParameterSyntax parameter,
		Span<int> transitionIndexes,
		ref int paramIndex,
		Func<int, FormatterModel, object> castGetter);

	public static void WriteTransitionOverload(
		this SourceBuilder headerBuilder,
		TransitionWriter transitionWriter,
		SourceBuilder bodyBuilder,
		XmlDocumentation xmlDocumentation,
		GeneratorProperties props,
		in SeparatedSyntaxList<ParameterSyntax> parameters,
		Span<int> transitionIndexes,
		Func<int, FormatterModel, object> castGetter,
		bool combineWithMode = false)
	{
		for (int index = 0, paramIndex = 0;;)
		{
			var mappedParam = props.Store.OverloadMap![index];
			var parameter = parameters[index];

			if (!combineWithMode || mappedParam.IsCombineNotExists)
			{
				transitionWriter(headerBuilder, bodyBuilder, xmlDocumentation, props, mappedParam, parameter, transitionIndexes, ref paramIndex, castGetter);

				if (++index == parameters.Count) break;
				if (!combineWithMode || props.Store.OverloadMap[index].IsCombineNotExists)
					headerBuilder
						.AppendAsConstant(",")
						.WhiteSpace();
				bodyBuilder
					.AppendAsConstant(",")
					.WhiteSpace();
			}
			else
			{
				int tempParamIndex = 0;
				for (int combinedIndex = 0; combinedIndex < mappedParam.CombineIndex; combinedIndex++)
				{
					var param = props.Store.OverloadMap[combinedIndex];
					if (param is {ParameterAction: ParameterAction.FormatterReplacement, IsCombineNotExists: false}) tempParamIndex++;
				}

				transitionWriter(EmptySourceBuilder.Instance,
					bodyBuilder,
					xmlDocumentation,
					props,
					props.Store.OverloadMap![mappedParam.CombineIndex],
					parameters[mappedParam.CombineIndex],
					transitionIndexes,
					ref tempParamIndex,
					castGetter);

				if (++index == parameters.Count) break;
				bodyBuilder
					.AppendAsConstant(",")
					.WhiteSpace();
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
				string[] decompositionParams = EmptySourceBuilder.Instance
					.AppendFormatterParam(props,
						props.Store.OverloadMap![mappedParam.CombineIndex].Type,
						parameter.Identifier.ValueText)
					.PickResult(parameter);
				bodyBuilder.Append(string.Join(", ", decompositionParams));
				break;
			}
			case ParameterAction.Nothing:
			case ParameterAction.SimpleReplacement:
			case ParameterAction.CustomReplacement:
			case ParameterAction.FormatterIntegrityReplacement:
			default:
				bodyBuilder.AppendCombinedSimple(mappedParam, parameter);
				break;
		}
	}

	public static void AppendCombinedSimple(this SourceBuilder bodyBuilder, ParameterData mappedParam, ParameterSyntax parameter)
	{
		if (mappedParam.Type.IsRefLikeType)
			bodyBuilder
				.AppendAsConstant("ref")
				.WhiteSpace();
		bodyBuilder.Append(parameter.Identifier.ValueText);
	}

	public static void AppendVariableToBody(this SourceBuilder bodyBuilder, ParameterSyntax parameter, string? paramName = null)
	{
		if (parameter.Modifiers.Any(SyntaxKind.RefKeyword))
			bodyBuilder
				.AppendAsConstant("ref")
				.WhiteSpace();
		bodyBuilder.Append(paramName ?? parameter.Identifier.ValueText);
	}
}
