using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Entities.Builders;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain.ChainUtils;

internal static partial class TransitionExtensions
{
	public static readonly TransitionWriter IntegrityParamTransitionOverloadWriter = WriteIntegrityParamTransitionOverload;

	private static void WriteIntegrityParamTransitionOverload(
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
			case ParameterAction.FormatterReplacement:
			case ParameterAction.FormatterIntegrityReplacement:
				if (!props.TryGetFormatter(parameter.GetType(props.Compilation), out var formatter))
					throw new ArgumentException("Unexpected exception. Formatters changed in real time.")
						.WithLocation(parameter);

				int transitionIndex = transitionIndexes[paramIndex++];
				if (transitionIndex == -1)
				{
					headerBuilder.AppendIntegrityParam(props, mappedParam, parameter);
					bodyBuilder.AppendVariableToBody(parameter, paramName);
					break;
				}

				var transition = formatter.IntegrityTransitions.Span[transitionIndex];
				var paramType = props.SetDeepestType(
					transition.TemplateType,
					props.Template,
					transition.TemplateType);

				headerBuilder
					.AppendWoTrim(mappedParam.BuildModifiersWithWhitespace(parameter, paramType))
					.AppendWith(paramType.ToDisplayString(), " ")
					.Append(paramName);

				string cast = transition.IntegrityCastCodeTemplate
					.Replace("${Var}", paramName)
					.Replace("${T}", props.Template.ToDisplayString());
				bodyBuilder.Append(cast);

				break;
			default:
				throw new ArgumentException($"Can't find case for {mappedParam.ParameterAction} parameterAction.")
					.WithLocation(parameter);
		}
	}
}
