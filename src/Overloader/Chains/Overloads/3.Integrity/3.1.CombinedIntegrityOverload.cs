using Overloader.Chains.Overloads.Overloads;
using Overloader.Chains.Overloads.Utils;
using Overloader.ContentBuilders;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Exceptions;

namespace Overloader.Chains.Overloads;

public sealed class CombinedIntegrityOverload : ArrowMethodOverloader, IChainMember
{
	ChainAction IChainMember.Execute(GeneratorProperties props)
	{
		if (props.Store.OverloadMap is null
		    || !props.Store.IsSmthChanged
		    || props.Store.CombineParametersCount == 0)
			return ChainAction.NextMember;
		
		WriteMethodOverload(
			props,
			XmlDocumentation.Parse(props.Store.MethodSyntax.GetLeadingTrivia()));

		return ChainAction.NextMember;
	}

	protected override void ParameterSeparatorAppender(
		GeneratorProperties props,
		SourceBuilder head,
		SourceBuilder body,
		int paramIndex)
	{
		var mappedParam = props.Store.OverloadMap[paramIndex];
		if (mappedParam.IsCombineNotExists)
			head.AppendAsConstant(", ");
		body.AppendAsConstant(", ");
	}

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
		
		if (!mappedParam.IsCombineNotExists)
		{
			body.AppendCombinedWoFormatter(
				props.Store.OverloadMap[mappedParam.CombineIndex],
				props.Store.MethodSyntax.ParameterList.Parameters[mappedParam.CombineIndex]);
			return;
		}
		
		string paramName = parameter.Identifier.ToString();
		switch (mappedParam.ReplacementType)
		{
			case ParameterReplacement.None:
				head.TrimAppend(parameter.ToFullString());
				break;
			case ParameterReplacement.Template:
			case ParameterReplacement.UserType:
				head.AppendParameter(parameter, mappedParam, props.Compilation);
				break;
			case ParameterReplacement.FormatterIntegrity:
			case ParameterReplacement.Formatter:
				head.AppendIntegrityParam(props, mappedParam, parameter);
				break;
			default:
				throw new ArgumentException($"Can't find case for '{mappedParam.ReplacementType}' parameter action.")
					.WithLocation(parameter);
		}
		xmlDocumentation.AddOverload(paramName, paramName);
		body.AppendVariableToBody(parameter, paramName);
	}
}
