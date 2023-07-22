using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.MethodWorkerChain.ChainUtils;
using Overloader.ContentBuilders;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Models;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain;

public sealed class CombinedDecompositionOverload : IChainMember
{
	ChainAction IChainMember.Execute(GeneratorProperties props, SyntaxNode syntaxNode)
	{
		if (props.Store.OverloadMap is null
		    || !props.Store.IsSmthChanged
		    || props.StartEntry.IgnoreTransitions
		    || props.Store.FormattersWoIntegrityCount == 0
		    || props.Store.CombineParametersCount == 0)
			return ChainAction.NextMember;

		if (!props.Store.OverloadMap.Any(param =>
			    param.ParameterAction is ParameterAction.FormatterReplacement
			    && param.IsCombineNotExists))
			return ChainAction.NextMember;

		var entry = (MethodDeclarationSyntax) syntaxNode;
		var parameters = entry.ParameterList.Parameters;

		using var bodyBuilder = SourceBuilder.GetInstance();
		bodyBuilder
			.Append(entry.Identifier.ToString())
			.Append("(");

		props.Builder
			.AppendChainMemberNameComment(nameof(CombinedDecompositionOverload))
			.AppendMethodDeclarationSpecifics(entry, props.Store.MethodData)
			.Append("(");

		for (int index = 0;;)
		{
			var parameter = parameters[index];
			var mappedParam = props.Store.OverloadMap[index];
			if (mappedParam.IsCombineNotExists)
			{
				string paramName = parameter.Identifier.ToString();
				switch (mappedParam.ParameterAction)
				{
					case ParameterAction.Nothing:
						props.Builder.Append(parameter.ToFullString());
						bodyBuilder.AppendVariableToBody(parameter, paramName);
						break;
					case ParameterAction.SimpleReplacement:
					case ParameterAction.CustomReplacement:
						props.Builder.AppendParameter(parameter, mappedParam, props.Compilation);
						bodyBuilder.AppendVariableToBody(parameter, paramName);
						break;
					case ParameterAction.FormatterIntegrityReplacement:
						props.Builder.AppendIntegrityParam(props, mappedParam, parameter);
						bodyBuilder.AppendVariableToBody(parameter, paramName);
						break;
					case ParameterAction.FormatterReplacement:
						bodyBuilder.AppendWoTrim(props.Builder
							.AppendFormatterParam(
								props,
								mappedParam.Type,
								paramName)
							.PickResult(parameter));
						break;
					default:
						throw new ArgumentException($"Can't find case for {props.Store.OverloadMap[index]} parameterAction.")
							.WithLocation(parameter);
				}

				if (++index == parameters.Count) break;
				if (props.Store.OverloadMap[index].IsCombineNotExists)
					props.Builder.AppendWoTrim(", ");
				bodyBuilder.AppendWoTrim(", ");
			}
			else
			{
				bodyBuilder.AppendCombined(props, mappedParam, parameters[mappedParam.CombineIndex]);
				if (++index == parameters.Count) break;
				bodyBuilder.AppendWoTrim(", ");
			}
		}

		props.Builder
			.AppendWith(")", " ")
			.Append(entry.ConstraintClauses.ToString());

		if (props.Store.IsNeedToRemoveBody)
			props.Builder.Append(";");
		else
			props.Builder.Append(" =>", 1)
				.NestedIncrease()
				.AppendRefReturnValues(entry.ReturnType)
				.Append(bodyBuilder.ToString())
				.AppendWoTrim(");", 1)
				.NestedDecrease();

		return ChainAction.NextMember;
	}
}
