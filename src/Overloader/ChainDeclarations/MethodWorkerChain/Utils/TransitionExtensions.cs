using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain.Utils;

internal static class TransitionExtensions
{
	public static void WriteOverload(this SourceBuilder headerBuilder,
		SourceBuilder bodyBuilder,
		GeneratorProperties props,
		SeparatedSyntaxList<ParameterSyntax> parameters,
		Span<int> transitionIndexes,
		bool combineWithMode = false)
	{
		for (int index = 0, paramIndex = 0; index < parameters.Count; index++)
		{
			var mappedParam = props.Store.OverloadMap![index];
			var parameter = parameters[index];

			if (combineWithMode && mappedParam.CombineWith is null)
			{
				string paramName = parameter.Identifier.ToString();
				switch (mappedParam.ParameterAction)
				{
					case ParameterAction.FormatterIntegrityReplacement when props.Template is null:
					case ParameterAction.Nothing:
						headerBuilder.Append(parameter.ToFullString());
						bodyBuilder.AppendWoTrim(paramName);
						break;
					case ParameterAction.SimpleReplacement:
					case ParameterAction.CustomReplacement:
						headerBuilder.AppendParameter(parameter, mappedParam.Type, props.Compilation);
						bodyBuilder.AppendWoTrim(paramName);
						break;
					case ParameterAction.FormatterIntegrityReplacement:
						headerBuilder.AppendIntegrityParam(props, mappedParam.Type, parameter);
						bodyBuilder.AppendWoTrim(paramName);
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
				headerBuilder.AppendWoTrim(", ");
				bodyBuilder.AppendWoTrim(", ");
			}
			else
			{
				bodyBuilder.AppendWoTrim(mappedParam.CombineWith);
				if (++index == parameters.Count) break;
				bodyBuilder.AppendWoTrim(", ");
			}
		}
	}
}
