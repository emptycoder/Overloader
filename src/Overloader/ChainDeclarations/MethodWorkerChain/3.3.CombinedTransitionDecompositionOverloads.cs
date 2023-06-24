﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.MethodWorkerChain.ChainUtils;
using Overloader.ContentBuilders;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Models;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain;

public sealed class CombinedTransitionDecompositionOverloads : IChainMember
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

		Span<int> maxTransitionsCount = stackalloc int[props.Store.FormattersWoIntegrityCount];
		ushort countOfCombineWith = 0;
		for (int index = 0, formatterIndex = 0; index < parameters.Count; index++)
		{
			var parameter = parameters[index];
			var mappedParam = props.Store.OverloadMap![index];
			if (mappedParam.ParameterAction is not ParameterAction.FormatterReplacement) continue;
			if (!mappedParam.IsCombineNotExists)
			{
				countOfCombineWith++;
				continue;
			}

			if (!props.TryGetFormatter(parameter.GetType(props.Compilation), out var formatter))
				throw new ArgumentException($"Formatter not found for {parameter.Identifier.ToString()}")
					.WithLocation(parameter.GetLocation());
			maxTransitionsCount[formatterIndex++] = formatter.DecompositionTransitions.Length;
		}

		maxTransitionsCount = maxTransitionsCount.Slice(0, props.Store.FormattersWoIntegrityCount - countOfCombineWith);

		if (maxTransitionsCount.Length == 0 || countOfCombineWith == 0) return ChainAction.NextMember;
		// Check that transitions exists
		for (int index = 0;;)
		{
			if (maxTransitionsCount[index] != 0) break;
			if (++index == maxTransitionsCount.Length) return ChainAction.NextMember;
		}

		Span<int> transitionIndexes = stackalloc int[maxTransitionsCount.Length];
		using var bodyBuilder = SourceBuilder.GetInstance();
		for (;;)
		{
			bodyBuilder.Append(entry.Identifier.ToString())
				.AppendWoTrim("(");
			props.Builder
				.AppendChainMemberNameComment(nameof(CombinedTransitionDecompositionOverloads))
				.AppendMethodDeclarationSpecifics(entry, props.Store.MethodData)
				.Append("(");
			props.Builder.WriteTransitionOverload(
				TransitionExtensions.ParamTransitionOverloadWriter,
				bodyBuilder,
				props,
				parameters,
				transitionIndexes,
				true);
			props.Builder
				.AppendWith(")", " ")
				.Append(entry.ConstraintClauses.ToString());

			if (props.Store.IsNeedToRemoveBody)
				props.Builder.Append(";");
			else
				props.Builder.Append(" =>", 1)
					.NestedIncrease()
					.AppendRefReturnValues(entry.ReturnType)
					.Append(bodyBuilder.ToStringAndClear())
					.AppendWoTrim(");", 1)
					.NestedDecrease();

			/*
				0 0 0 0 0
				^

				1 0 0 0 0
				^
				Repeat until it < maxLength[index]
				And when first value equals maxLength[index] reset to zero and add 1 to next rank
				0 1 0 0 0
				^
				
				1 1 0 0 0
				^
				And so on...
			 */
			for (int index = 0;;)
			{
				if (++transitionIndexes[index] != maxTransitionsCount[index]) break;
				transitionIndexes[index] = 0;

				if (++index == transitionIndexes.Length)
					return ChainAction.NextMember;
			}
		}
	}
}