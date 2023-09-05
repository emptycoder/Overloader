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
		int paramIndex)
	{
		var mappedParam = props.Store.OverloadMap[paramIndex];
		var parameter = props.Store.MethodSyntax.ParameterList.Parameters[paramIndex];
		string paramName = parameter.Identifier.ToString();
		switch (mappedParam.ReplacementType)
		{
			case ParameterReplacement.None:
				head.TrimAppend(parameter.ToFullString());
				body.AppendVariableToBody(parameter, paramName);
				break;
			case ParameterReplacement.Template:
			case ParameterReplacement.UserType:
				head.AppendParameter(parameter, mappedParam, props.Compilation);
				body.AppendVariableToBody(parameter, paramName);
				break;
			case ParameterReplacement.Formatter:
			case ParameterReplacement.FormatterIntegrity:
				if (!props.TryGetFormatter(parameter.GetType(props.Compilation).GetClearType(), out var formatter))
					throw new ArgumentException("Unexpected exception. Formatters have been changed in real time.")
						.WithLocation(parameter);
				
				int transitionIndex = indexes[paramIndex];
				if (transitionIndex == -1)
				{
					head.AppendIntegrityParam(props, mappedParam, parameter);
					body.AppendVariableToBody(parameter, paramName);
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
								props.Template,
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

				body.TrimAppend(cast.Replace("${T}", props.Template.ToDisplayString()));
				return;
			default:
				throw new ArgumentException($"Can't find case for {mappedParam.ReplacementType} parameterAction.")
					.WithLocation(parameter);
		}
		
		xmlDocumentation.AddOverload(paramName, paramName);
	}
}
