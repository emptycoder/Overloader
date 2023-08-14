using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ContentBuilders;
using Overloader.Entities;
using Overloader.Entities.Formatters;
using Overloader.Entities.Formatters.Transitions;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.Overloads.Utils;

public static partial class TransitionExtensions
{
	public static void WriteCastTransitionOverload(
		SourceBuilder headerBuilder,
		SourceBuilder bodyBuilder,
		XmlDocumentation xmlDocumentation,
		GeneratorProperties props,
		ParameterData mappedParam,
		ParameterSyntax parameter,
		Span<int> transitionIndexes,
		ref int paramIndex,
		Func<int, FormatterModel, object> castGetter)
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
				
				var transition = (CastModel) castGetter(transitionIndex, formatter);
				string cast = transition.CastCodeTemplate;
				for (int index = 0;;)
				{
					var castedParameter = transition.Types[index];
					var paramType = castedParameter.IsUnboundType
						? props
							.SetDeepestType(
								castedParameter.Type,
								props.Template,
								castedParameter.Type)
							.PickResult(parameter)
						: castedParameter.Type;

					string strIndex = index.ToString();
					string indexedParameterName = $"{castedParameter.Name}To{char.ToUpper(paramName[0]).ToString()}{paramName.AsSpan(1).ToString()}";
					xmlDocumentation.AddOverload(paramName, indexedParameterName);
					headerBuilder
						.Append(castedParameter.Modifier)
						.Append(paramType.ToDisplayString())
						.WhiteSpace()
						.Append(indexedParameterName);
					cast = cast.Replace($"${{Var{strIndex}}}", indexedParameterName);

					if (++index == transition.Types.Length)
						break;
					headerBuilder
						.AppendAsConstant(",")
						.WhiteSpace();
				}

				bodyBuilder.Append(cast.Replace("${T}", props.Template.ToDisplayString()));
				break;
			default:
				throw new ArgumentException($"Can't find case for {mappedParam.ParameterAction} parameterAction.")
					.WithLocation(parameter);
		}
	}
}
