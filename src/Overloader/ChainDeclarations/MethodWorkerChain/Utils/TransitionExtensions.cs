using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Entities.Builders;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain.Utils;

internal static class TransitionExtensions
{
	public static void WriteTransitionOverload(this SourceBuilder headerBuilder,
		SourceBuilder bodyBuilder,
		GeneratorProperties props,
		in SeparatedSyntaxList<ParameterSyntax> parameters,
		Span<int> transitionIndexes,
		bool combineWithMode = false)
	{
		for (int index = 0, paramIndex = 0; index < parameters.Count; index++)
		{
			var mappedParam = props.Store.OverloadMap![index];
			var parameter = parameters[index];

			if (!combineWithMode || mappedParam.CombineIndex == -1)
			{
				string paramName = parameter.Identifier.ToString();
				switch (mappedParam.ParameterAction)
				{
					case ParameterAction.FormatterIntegrityReplacement when props.Template is null:
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

						// Build header for transition method
						var transition = formatter.Transitions[transitionIndexes[paramIndex++]];
						for (int linkIndex = 0;;)
						{
							var transitionLink = transition.Links[linkIndex];
							string variableName = $"{paramName}{linkIndex.ToString()}";
							headerBuilder.AppendWith(transitionLink.Type.ToDisplayString(), " ")
								.Append(variableName);

							if (++linkIndex == transition.Links.Length) break;
							headerBuilder.AppendWoTrim(", ");
						}

						// Build body for transition method-
						for (int transitionParamIndex = 0;;)
						{
							var formatterParam = formatter.Params[transitionParamIndex];
							var replacement = transition.FindReplacement(formatterParam.Name, out int linkIndex);
							bodyBuilder.Append($"{paramName}{linkIndex.ToString()}.{replacement}");

							if (++transitionParamIndex == formatter.Params.Length) break;
							bodyBuilder.AppendWoTrim(", ");
						}

						break;
					default:
						throw new ArgumentException($"Can't find case for {props.Store.OverloadMap[index]} parameterAction.")
							.WithLocation(parameter);
				}
				
				if (++index == parameters.Count) break;
				if (!combineWithMode || props.Store.OverloadMap[index].CombineIndex == -1)
					headerBuilder.AppendWoTrim(", ");
				bodyBuilder.AppendWoTrim(", ");
			}
			else
			{
				bodyBuilder.AppendCombined(props, mappedParam, parameters[mappedParam.CombineIndex]);
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
