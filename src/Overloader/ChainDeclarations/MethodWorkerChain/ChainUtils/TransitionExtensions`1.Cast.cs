using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ContentBuilders;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Models;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain.ChainUtils;

public static partial class TransitionExtensions
{
	public static void WriteCastTransitionOverload(
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
				if (!props.TryGetFormatter(parameter.GetType(props.Compilation).GetClearType(), out var formatter))
					throw new ArgumentException("Unexpected exception. Formatters have been changed in real time.")
						.WithLocation(parameter);

				int transitionIndex = transitionIndexes[paramIndex++];
				if (transitionIndex == -1)
				{
					headerBuilder.AppendIntegrityParam(props, mappedParam, parameter);
					bodyBuilder.AppendVariableToBody(parameter, paramName);
					break;
				}

				var transition = formatter.CastTransitions.Span[transitionIndex];
				string cast = transition.IntegrityCastCodeTemplate;
				for (int index = 0;;)
				{
					var template = transition.Templates[index];
					var paramType = template.IsUnboundTemplateGenericType
						? props
							.SetDeepestType(
								template.Type,
								props.Template,
								template.Type)
							.PickResult(parameter)
						: template.Type;

					string strIndex = index.ToString();
					string indexedParamName = $"{paramName}Cast{strIndex}";
					headerBuilder
						.AppendWoTrim(mappedParam.BuildModifiersWithWhitespace(parameter, paramType))
						.AppendWith(paramType.ToDisplayString(), " ")
						.Append(indexedParamName);
					cast = cast.Replace($"${{Var{strIndex}}}", indexedParamName);

					if (++index == transition.Templates.Length)
						break;
					headerBuilder.AppendWoTrim(", ");
				}

				bodyBuilder.Append(cast.Replace("${T}", props.Template.ToDisplayString()));
				break;
			default:
				throw new ArgumentException($"Can't find case for {mappedParam.ParameterAction} parameterAction.")
					.WithLocation(parameter);
		}
	}
}
