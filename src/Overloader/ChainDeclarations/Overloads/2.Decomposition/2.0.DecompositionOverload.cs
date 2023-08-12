using System.Buffers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.Overloads.Utils;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Exceptions;

namespace Overloader.ChainDeclarations.Overloads;

public sealed class DecompositionOverload : IChainMember
{
	ChainAction IChainMember.Execute(GeneratorProperties props, SyntaxNode syntaxNode)
	{
		if (props.Store.OverloadMap is null
		    || !props.Store.IsSmthChanged
		    || props.StartEntry.IgnoreTransitions
		    || props.Store.FormattersWoIntegrityCount == 0)
			return ChainAction.NextMember;

		var entry = (MethodDeclarationSyntax) syntaxNode;
		var parameters = entry.ParameterList.Parameters;

		int replacementVariableIndex = 0;
		var replacementVariableNames = ArrayPool<(string Replacement, string ConcatedParams)>
			.Shared.Rent(props.Store.FormattersWoIntegrityCount);

		props.Builder
			.AppendChainMemberNameComment(nameof(DecompositionOverload))
			.AppendMethodDeclarationSpecifics(entry, props.Store.MethodData)
			.Append("(");

		for (int index = 0;;)
		{
			var mappedParam = props.Store.OverloadMap[index];
			var parameter = parameters[index];

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
					props.Builder.AppendIntegrityParam(props, mappedParam, parameter);
					break;
				case ParameterAction.FormatterReplacement:
					string paramName = parameter.Identifier.ToString();
					string concatedParams = props.Builder
						.AppendFormatterParam(
							props,
							mappedParam.Type,
							paramName)
						.PickResult(parameter);
					replacementVariableNames[replacementVariableIndex++] = (paramName, concatedParams);
					break;
				default:
					throw new ArgumentException($"Can't find case for {props.Store.OverloadMap[index]} parameterAction.")
						.WithLocation(parameter);
			}

			if (++index == parameters.Count) break;
			props.Builder.AppendWoTrim(", ");
		}

		props.Builder
			.AppendWith(")", " ")
			.Append(entry.ConstraintClauses.ToString());
		props.WriteMethodBody(entry, replacementVariableNames.AsSpan(0, replacementVariableIndex));
		ArrayPool<(string, string)>.Shared.Return(replacementVariableNames);

		return ChainAction.NextMember;
	}
}
