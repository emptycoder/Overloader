using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.Overloads.Utils;
using Overloader.ContentBuilders;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Exceptions;
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

		using var bodyBuilder = StringSourceBuilder.Instance;
		bodyBuilder.Append(entry.Identifier.ToString())
			.AppendAsConstant("(");

		props.Builder
			.AppendChainMemberNameComment(nameof(CombinedIntegrityOverload))
			.Append(entry.GetLeadingTrivia().ToString(), 1)
			.AppendMethodDeclarationSpecifics(entry, props.Store.MethodData)
			.AppendAsConstant("(");

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
					props.Builder
						.AppendAsConstant(",")
						.WhiteSpace();
				bodyBuilder
					.AppendAsConstant(",")
					.WhiteSpace();
			}
			else
			{
				bodyBuilder.AppendCombinedSimple(mappedParam, parameters[mappedParam.CombineIndex]);
				if (++index == parameters.Count) break;
				if (props.Store.OverloadMap[index].IsCombineNotExists)
					props.Builder
						.AppendAsConstant(",")
						.WhiteSpace();
				bodyBuilder
					.AppendAsConstant(",")
					.WhiteSpace();
			}
		}

		CloseParameterBracket:
		props.Builder
			.AppendAsConstant(")")
			.WhiteSpace()
			.Append(entry.ConstraintClauses.ToString());

		if (props.Store.IsNeedToRemoveBody)
			props.Builder.AppendAsConstant(";");
		else
			props.Builder
				.WhiteSpace()
				.AppendAsConstant("=>", 1)
				.NestedIncrease()
				.AppendRefReturnValues(entry.ReturnType)
				.Append(bodyBuilder)
				.AppendAsConstant(");", 1)
				.NestedDecrease();

		return ChainAction.NextMember;
	}
}
