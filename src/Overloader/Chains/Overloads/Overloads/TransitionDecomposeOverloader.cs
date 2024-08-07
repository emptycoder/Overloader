using Microsoft.CodeAnalysis;
using Overloader.Chains.Overloads.Utils;
using Overloader.ContentBuilders;
using Overloader.Entities;
using Overloader.Entities.DTOs.Attributes.Formatters.Transitions;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Chains.Overloads.Overloads;

public abstract class TransitionDecomposeOverloader : ArrowMethodOverloader
{
	protected override void WriteParameter(
		GeneratorProperties props,
		SourceBuilder head,
		SourceBuilder body,
		XmlDocumentation xmlDocumentation,
		Span<int> indexes,
		Span<int> maxIndexesCount,
		int paramIndex)
	{
		var mappedParam = props.Store.MethodDataDto.Parameters[paramIndex];
		var parameter = props.Store.MethodSyntax.ParameterList.Parameters[paramIndex];
		string paramName = parameter.Identifier.ToString();
		switch (mappedParam.ReplacementType)
		{
			case RequiredReplacement.None:
				head.TrimAppend(parameter.ToFullString());
				body.AppendParameterWoFormatter(parameter, paramName: paramName);
				break;
			case RequiredReplacement.Template:
			case RequiredReplacement.UserType:
				head.AppendParameter(parameter, mappedParam, props.Compilation);
				body.AppendParameterWoFormatter(parameter, paramName: paramName);
				break;
			case RequiredReplacement.FormatterIntegrity:
				head.AppendIntegrityParam(props, mappedParam, parameter);
				body.AppendParameterWoFormatter(parameter, paramName: paramName);
				break;
			case RequiredReplacement.Formatter:
				if (!props.TryGetFormatter(parameter.GetType(props.Compilation), out var formatter))
					throw new ArgumentException("Formatters changed after analyzing stage.")
						.NotExpected()
						.WithLocation(parameter);

				DecomposeTransitionDto? transition = null;
				int transitionIndex = indexes[paramIndex];
				if (transitionIndex >= formatter.Decompositions.Count)
				{
					head.AppendIntegrityParam(props, mappedParam, parameter);
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
							props.Templates[mappedParam.TemplateIndex],
							transitionLink.TemplateType).PickResult(parameter);

						if (paramType is {IsValueType: true, SpecialType: SpecialType.System_ValueType})
							head
								.AppendAsConstant("in")
								.WhiteSpace();

						head
							.TrimAppend(paramType.ToDisplayString())
							.WhiteSpace()
							.TrimAppend(paramName)
							.TrimAppend(linkIndex.ToString());
						xmlDocumentation.AddOverload(paramName, $"{paramName}{linkIndex.ToString()}");

						if (++linkIndex == transition.Links.Length) break;
						head
							.AppendAsConstant(",")
							.WhiteSpace();
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
						string newParamName = $"{paramName}{linkIndex.ToString()}";
						body.TrimAppend(newParamName)
							.AppendAsConstant(".")
							.TrimAppend(replacement ?? string.Empty);
					}
					else
					{
						var templateType = formatterParam.Param.GetType(props.Templates[mappedParam.TemplateIndex]);
						var paramType = props.SetDeepestTypeWithTemplateFilling(
								templateType,
								props.Templates[mappedParam.TemplateIndex])
							.PickResult(parameter);

						head
							.AppendAsConstant(",")
							.WhiteSpace()
							.TrimAppend(paramType.ToDisplayString())
							.WhiteSpace()
							.TrimAppend(paramName)
							.TrimAppend(formatterParam.Identifier);
						xmlDocumentation.AddOverload(paramName, $"{paramName}{formatterParam.Identifier}");

						body
							.TrimAppend(paramName)
							.TrimAppend(formatterParam.Identifier);
					}

					if (++formatterParamIndex == formatter.Params.Length) break;
					body
						.AppendAsConstant(",")
						.WhiteSpace();
				}

				return;
			default:
				throw new ArgumentException($"Can't find case for {mappedParam.ReplacementType} parameterAction.")
					.NotExpected()
					.WithLocation(parameter);
		}

		xmlDocumentation.AddOverload(paramName, paramName);
	}
}
