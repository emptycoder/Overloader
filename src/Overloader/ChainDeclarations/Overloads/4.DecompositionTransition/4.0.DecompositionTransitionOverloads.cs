﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.Overloads.Utils;
using Overloader.ContentBuilders;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.Overloads;

public sealed class DecompositionTransitionOverloads : IChainMember
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
		for (int index = 0, formatterIndex = 0; index < parameters.Count; index++)
		{
			var parameter = parameters[index];
			var mappedParam = props.Store.OverloadMap[index];
			if (mappedParam.ParameterAction is not ParameterAction.FormatterReplacement) continue;
			if (!props.TryGetFormatter(parameter.GetType(props.Compilation).GetClearType(), out var formatter))
				throw new ArgumentException($"Formatter not found for {parameter.Identifier.ToString()}")
					.WithLocation(parameter.GetLocation());
			maxTransitionsCount[formatterIndex++] = formatter.Decompositions.Count;
		}

		// Check that transitions exist
		for (int index = 0;;)
		{
			if (maxTransitionsCount[index] != 0) break;
			if (++index == maxTransitionsCount.Length) return ChainAction.NextMember;
		}

		var xmlDocumentation = XmlDocumentation.Parse(entry.GetLeadingTrivia());
		Span<int> transitionIndexes = stackalloc int[props.Store.FormattersWoIntegrityCount];
		using var bodyBuilder = StringSourceBuilder.Instance;
		using var parameterBuilder = StringSourceBuilder.Instance;
		for (;;)
		{
			bodyBuilder.Append(entry.Identifier.ToString())
				.AppendAsConstant("(");
			props.Builder
				.AppendChainMemberNameComment(nameof(DecompositionTransitionOverloads));
			
			parameterBuilder
				.AppendMethodDeclarationSpecifics(entry, props.Store.MethodData)
				.AppendAsConstant("(")
				.WriteTransitionOverload(
					TransitionExtensions.WriteDecompositionTransitionOverload,
					bodyBuilder,
					xmlDocumentation,
					props,
					parameters,
					transitionIndexes,
					(index, formatter) => formatter.Decompositions[index]);
			
			props.Builder
				.AppendXmlDocumentation(xmlDocumentation)
				.BreakLine()
				.AppendAndClear(parameterBuilder)
				.AppendAsConstant(")")
				.WhiteSpace()
				.Append(entry.ConstraintClauses.ToString());

			if (props.Store.IsNeedToRemoveBody)
				props.Builder.AppendAsConstant(";");
			else
				props.Builder
					.AppendAsConstant("=>", 1)
					.NestedIncrease()
					.AppendRefReturnValues(entry.ReturnType)
					.AppendAndClear(bodyBuilder)
					.AppendAsConstant(");", 1)
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
				if (transitionIndexes[index] != maxTransitionsCount[index]
				    && ++transitionIndexes[index] != maxTransitionsCount[index]) break;
				transitionIndexes[index] = 0;

				if (++index == transitionIndexes.Length)
					return ChainAction.NextMember;
			}
		}
	}
}
