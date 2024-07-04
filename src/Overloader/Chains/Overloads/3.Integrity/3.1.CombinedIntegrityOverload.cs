using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
		if (props.Store.CombineParametersCount == 0)
			return ChainAction.NextMember;

		WriteMethodOverload(props);
		return ChainAction.NextMember;
	}

	protected override void ParameterSeparatorAppender(
		GeneratorProperties props,
		SourceBuilder head,
		SourceBuilder body,
		int paramIndex)
	{
		var mappedParam = props.Store.MethodData.Parameters[paramIndex];
		if (mappedParam.IsCombineNotExists)
			head.AppendAsConstant(",")
				.WhiteSpace();
		body.AppendAsConstant(",")
			.WhiteSpace();
	}

	protected override void WriteParameter(
		GeneratorProperties props,
		SourceBuilder head,
		SourceBuilder body,
		XmlDocumentation xmlDocumentation,
		Span<int> indexes,
		Span<int> maxIndexesCount,
		int paramIndex)
	{
		var mappedParam = props.Store.MethodData.Parameters[paramIndex];
		var parameter = props.Store.MethodSyntax.ParameterList.Parameters[paramIndex];

		if (!mappedParam.IsCombineNotExists)
		{
			body.AppendParameterWoFormatter(
				props.Store.MethodSyntax.ParameterList.Parameters[mappedParam.CombineIndex],
				// Refer to ref attribute on not combined parameter due body apply logic
				parameter.Modifiers.Any(SyntaxKind.RefKeyword));
			return;
		}

		string paramName = parameter.Identifier.ToString();
		switch (mappedParam.ReplacementType)
		{
			case RequiredReplacement.None:
				head.TrimAppend(parameter.ToFullString());
				break;
			case RequiredReplacement.Template:
			case RequiredReplacement.UserType:
				head.AppendParameter(parameter, mappedParam, props.Compilation);
				break;
			case RequiredReplacement.FormatterIntegrity:
			case RequiredReplacement.Formatter:
				head.AppendIntegrityParam(props, mappedParam, parameter);
				break;
			default:
				throw new ArgumentException($"Can't find case for '{mappedParam.ReplacementType}' parameter action.")
					.NotExpected()
					.WithLocation(parameter);
		}

		xmlDocumentation.AddOverload(paramName, paramName);
		body.AppendParameterWoFormatter(parameter, paramName: paramName);
	}
}
