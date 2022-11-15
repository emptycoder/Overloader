using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Entities.ContentBuilders;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain.ChainUtils;

internal static partial class TransitionExtensions
{
	public static readonly TransitionWriter ParamTransitionOverloadWriter = WriteParamTransitionOverload;

	private static void WriteParamTransitionOverload(SourceBuilder headerBuilder,
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

				var transition = formatter.DeconstructTransitions.Span[transitionIndexes[paramIndex++]];
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
						    formatterParam.Identifier,
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
