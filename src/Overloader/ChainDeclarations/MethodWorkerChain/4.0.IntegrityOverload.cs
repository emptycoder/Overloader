﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.MethodWorkerChain.ChainUtils;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Exceptions;

namespace Overloader.ChainDeclarations.MethodWorkerChain;

/// <summary>
///     Generate main type which doesn't decompose method params using formatters
/// </summary>
internal sealed class IntegrityOverload : IChainMember
{
	ChainAction IChainMember.Execute(GeneratorProperties props, SyntaxNode syntaxNode)
	{
		if (props.Store.OverloadMap is null
		    || props.Store.Modifiers is null
		    || !props.Store.IsSmthChanged
		    || props.IsTSpecified)
			return ChainAction.NextMember;

		var entry = (MethodDeclarationSyntax) syntaxNode;
		var parameters = entry.ParameterList.Parameters;

		props.Builder
			.AppendChainMemberNameComment(nameof(IntegrityOverload))
			.AppendMethodDeclarationSpecifics(entry, props.Store.Modifiers, props.Store.ReturnType)
			.Append("(");

		if (parameters.Count == 0) goto CloseParameterBracket;

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
					props.Builder.AppendParameter(parameter, mappedParam.Type, props.Compilation);
					break;
				case ParameterAction.FormatterIntegrityReplacement:
				case ParameterAction.FormatterReplacement:
					props.Builder.AppendIntegrityParam(props, mappedParam.Type, parameter);
					break;
				default:
					throw new ArgumentException($"Can't find case for {props.Store.OverloadMap[index]} parameterAction.")
						.WithLocation(entry);
			}

			if (++index == parameters.Count) break;
			props.Builder.AppendWoTrim(", ");
		}

		CloseParameterBracket:
		props.Builder.AppendWoTrim(") ");
		props.WriteMethodBody(entry, Array.Empty<(string, string)>());

		return ChainAction.NextMember;
	}
}
