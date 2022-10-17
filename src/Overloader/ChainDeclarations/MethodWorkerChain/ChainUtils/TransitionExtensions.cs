using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Entities.Builders;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain.ChainUtils;

internal static class TransitionExtensions
{
	public static void WriteParamsTransitionOverload(this SourceBuilder headerBuilder,
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
				headerBuilder.WriteParamTransitionOverload(bodyBuilder, props, mappedParam, parameter, transitionIndexes, ref paramIndex);

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

				EmptySourceBuilder.Instance.WriteParamTransitionOverload(
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

	private static void WriteParamTransitionOverload(this SourceBuilder headerBuilder,
		SourceBuilder bodyBuilder,
		GeneratorProperties props,
		ParameterData mappedParam,
		ParameterSyntax parameter,
		Span<int> transitionIndexes,
		ref int paramIndex)
	{
		string paramName = parameter.Identifier.ToString();
		switch (mappedParam.ParameterAction)
		{
			// case ParameterAction.FormatterIntegrityReplacement when props.Template is null:
			case ParameterAction.Nothing:
				headerBuilder.Append(parameter.ToFullString());
				bodyBuilder.AppendVariableToBody(parameter, paramName);
				break;
			case ParameterAction.SimpleReplacement:
			case ParameterAction.CustomReplacement:
				headerBuilder.AppendParameter(parameter, mappedParam.Type, props.Compilation);
				bodyBuilder.AppendVariableToBody(parameter, paramName);
				break;
			case ParameterAction.FormatterIntegrityReplacement:
				headerBuilder.AppendIntegrityParam(props, mappedParam.Type, parameter);
				bodyBuilder.AppendVariableToBody(parameter, paramName);
				break;
			case ParameterAction.FormatterReplacement:
				if (!props.TryGetFormatter(parameter.GetType(props.Compilation), out var formatter))
					throw new ArgumentException("Unexpected exception. Formatters changed in real time.")
						.WithLocation(parameter);

				var transition = formatter.Transitions[transitionIndexes[paramIndex++]];
				// Build transition method header
				for (int linkIndex = 0;;)
				{
					var transitionLink = transition.Links[linkIndex];
					var paramType = props.SetDeepestType(
						transitionLink.TemplateType,
						props.Template,
						transitionLink.TemplateType);
					
					if (paramType.IsValueType && paramType.SpecialType == SpecialType.System_ValueType)
						headerBuilder.AppendWith("in", " ");

					headerBuilder
						.AppendWith(paramType.ToDisplayString(), " ")
						.Append(paramName)
						.Append(linkIndex.ToString());

					if (++linkIndex == transition.Links.Length) break;
					headerBuilder.AppendWoTrim(", ");
				}

				// Build transition method body and least header parts
				for (int formatterParamIndex = 0;;)
				{
					var formatterParam = formatter.Params[formatterParamIndex];
					if (transition.TryToFindReplacement(
						    formatterParam.Name,
						    out string? replacement,
						    out int linkIndex))
					{
						bodyBuilder.Append($"{paramName}{linkIndex.ToString()}.{replacement}");
					}
					else
					{
						var templateType = formatterParam.Param.GetType(props.Template);
						headerBuilder
							.AppendWoTrim(", ")
							.AppendWith(props.SetDeepestTypeWithTemplateFilling(templateType, props.Template).ToDisplayString(), " ")
							.Append(paramName)
							.Append(formatterParam.Name);

						bodyBuilder
							.AppendWoTrim(paramName)
							.AppendWoTrim(formatterParam.Name);
					}

					if (++formatterParamIndex == formatter.Params.Length) break;
					bodyBuilder.AppendWoTrim(", ");
				}

				break;
			default:
				throw new ArgumentException($"Can't find case for {mappedParam.ParameterAction} parameterAction.")
					.WithLocation(parameter);
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
