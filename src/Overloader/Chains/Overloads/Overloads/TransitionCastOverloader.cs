using Overloader.Chains.Overloads.Utils;
using Overloader.ContentBuilders;
using Overloader.Entities;
using Overloader.Entities.Formatters;
using Overloader.Entities.Formatters.Transitions;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Chains.Overloads.Overloads;

public abstract class TransitionCastOverloader : ArrowMethodOverloader
{
	protected abstract CastModel GetCastModel(FormatterModel formatter, int transitionIndex);

	protected override void WriteParameter(
		GeneratorProperties props,
		SourceBuilder head,
		SourceBuilder body,
		XmlDocumentation xmlDocumentation,
		Span<int> indexes,
		Span<int> maxIndexesCount,
		int paramIndex)
	{
		var mappedParam = props.Store.OverloadMap[paramIndex];
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
			case RequiredReplacement.Formatter:
			case RequiredReplacement.FormatterIntegrity:
				if (!props.TryGetFormatter(parameter.GetType(props.Compilation).GetClearType(), out var formatter))
					throw new ArgumentException("Unexpected exception. Formatters have been changed in real time.")
						.WithLocation(parameter);

				int transitionIndex = indexes[paramIndex];
				if (transitionIndex == -1)
				{
					head.AppendIntegrityParam(props, mappedParam, parameter);
					body.AppendParameterWoFormatter(parameter, paramName: paramName);
					return;
				}

				var transition = GetCastModel(formatter, transitionIndex);
				string cast = transition.CastCodeTemplate;
				for (int index = 0;;)
				{
					var castedParameter = transition.Types[index];
					var paramType = castedParameter.IsUnboundType
						? props
							.SetDeepestType(
								castedParameter.Type,
								props.Templates[mappedParam.TemplateIndex],
								castedParameter.Type)
							.PickResult(parameter)
						: castedParameter.Type;

					string strIndex = index.ToString();
					string indexedParameterName = $"{castedParameter.Name}To{char.ToUpper(paramName[0]).ToString()}{paramName.AsSpan(1).ToString()}";
					xmlDocumentation.AddOverload(paramName, indexedParameterName);
					head
						.TrimAppend(castedParameter.Modifier)
						.TrimAppend(paramType.ToDisplayString())
						.WhiteSpace()
						.TrimAppend(indexedParameterName);
					cast = cast.Replace($"${{Var{strIndex}}}", indexedParameterName);

					if (++index == transition.Types.Length)
						break;
					head
						.AppendAsConstant(",")
						.WhiteSpace();
				}

				body.TrimAppend(cast.Replace("${T}", props.Templates[mappedParam.TemplateIndex].ToDisplayString()));
				return;
			default:
				throw new ArgumentException($"Can't find case for {mappedParam.ReplacementType} parameterAction.")
					.Unreachable()
					.WithLocation(parameter);
		}

		xmlDocumentation.AddOverload(paramName, paramName);
	}
}
