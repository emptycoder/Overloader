using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.Overloads.Utils;
using Overloader.ContentBuilders;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Models;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.Overloads;

public sealed class CombinedIntegrityOverload : IChainMember
{
	ChainAction IChainMember.Execute(GeneratorProperties props, SyntaxNode syntaxNode)
	{
		if (props.Store.OverloadMap is null
		    || !props.Store.IsSmthChanged
		    || props.Store.CombineParametersCount == 0)
			return ChainAction.NextMember;

		var entry = (MethodDeclarationSyntax) syntaxNode;
		var parameters = entry.ParameterList.Parameters;

		using var bodyBuilder = SourceBuilder.GetInstance();
		bodyBuilder.Append(entry.Identifier.ToString())
			.AppendWoTrim("(");

		props.Builder
			.AppendChainMemberNameComment(nameof(CombinedIntegrityOverload))
			.AppendMethodDeclarationSpecifics(entry, props.Store.MethodData)
			.AppendWoTrim("(");

		if (parameters.Count == 0) goto CloseParameterBracket;

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
						break;
					case ParameterAction.SimpleReplacement:
					case ParameterAction.CustomReplacement:
						props.Builder.AppendParameter(parameter, mappedParam, props.Compilation);
						break;
					case ParameterAction.FormatterIntegrityReplacement:
					case ParameterAction.FormatterReplacement:
						props.Builder.AppendIntegrityParam(props, mappedParam, parameter);
						break;
					default:
						throw new ArgumentException($"Can't find case for {props.Store.OverloadMap[index]} parameterAction.")
							.WithLocation(entry);
				}

				bodyBuilder.AppendVariableToBody(parameter, paramName);

				if (++index == parameters.Count) break;
				if (props.Store.OverloadMap[index].IsCombineNotExists)
					props.Builder.AppendWoTrim(", ");
				bodyBuilder.AppendWoTrim(", ");
			}
			else
			{
				bodyBuilder.AppendCombinedSimple(mappedParam, parameters[mappedParam.CombineIndex]);
				if (++index == parameters.Count) break;
				if (props.Store.OverloadMap[index].IsCombineNotExists)
					props.Builder.AppendWoTrim(", ");
				bodyBuilder.AppendWoTrim(", ");
			}
		}

		CloseParameterBracket:
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
