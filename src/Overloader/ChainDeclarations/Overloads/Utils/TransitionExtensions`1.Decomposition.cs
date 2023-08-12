using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ContentBuilders;
using Overloader.Entities;
using Overloader.Entities.Formatters.Transitions;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.Overloads.Utils;

public static partial class TransitionExtensions
{
	public static void WriteDecompositionTransitionOverload(
		SourceBuilder headerBuilder,
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
			case ParameterAction.Nothing:
				headerBuilder.Append(parameter.ToFullString());
				bodyBuilder.AppendVariableToBody(parameter, paramName);
				break;
			case ParameterAction.SimpleReplacement:
			case ParameterAction.CustomReplacement:
				headerBuilder.AppendParameter(parameter, mappedParam, props.Compilation);
				bodyBuilder.AppendVariableToBody(parameter, paramName);
				break;
			case ParameterAction.FormatterIntegrityReplacement:
				headerBuilder.AppendIntegrityParam(props, mappedParam, parameter);
				bodyBuilder.AppendVariableToBody(parameter, paramName);
				break;
			case ParameterAction.FormatterReplacement:
				if (!props.TryGetFormatter(parameter.GetType(props.Compilation), out var formatter))
					throw new ArgumentException("Unexpected exception. Formatters changed in real time.")
						.WithLocation(parameter);
				var transitionIndex = transitionIndexes[paramIndex++];
				DecompositionModel? transition = null;
				if (transitionIndex >= formatter.Decompositions.Count)
				{
					headerBuilder.AppendIntegrityParam(props, mappedParam, parameter);
				}
				else
				{
					transition = formatter.Decompositions[transitionIndex];
					// Build transition method header
					for (int linkIndex = 0;;)
					{
						var transitionLink = transition.Links[linkIndex];
						var paramType = props.SetDeepestType(
							transitionLink.TemplateType,
							props.Template,
							transitionLink.TemplateType).PickResult(parameter);

						if (paramType is {IsValueType: true, SpecialType: SpecialType.System_ValueType})
							headerBuilder.AppendWith("in", " ");

						headerBuilder
							.AppendWith(paramType.ToDisplayString(), " ")
							.Append(paramName)
							.Append(linkIndex.ToString());

						if (++linkIndex == transition.Links.Length) break;
						headerBuilder.AppendWoTrim(", ");
					}
				}

				// Build transition method body and least header parts
				for (int formatterParamIndex = 0;;)
				{
					var formatterParam = formatter.Params[formatterParamIndex];
					if (transition is not null && transition.TryToFindReplacement(
						    formatterParam.Identifier,
						    out string? replacement,
						    out int linkIndex))
					{
						bodyBuilder.Append($"{paramName}{linkIndex.ToString()}.{replacement}");
					}
					else
					{
						var templateType = formatterParam.Param.GetType(props.Template);
						var paramType = props.SetDeepestTypeWithTemplateFilling(templateType, props.Template).PickResult(parameter);

						headerBuilder
							.AppendWoTrim(", ")
							.AppendWith(paramType.ToDisplayString(), " ")
							.Append(paramName)
							.Append(formatterParam.Identifier);

						bodyBuilder
							.AppendWoTrim(paramName)
							.AppendWoTrim(formatterParam.Identifier);
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
}
