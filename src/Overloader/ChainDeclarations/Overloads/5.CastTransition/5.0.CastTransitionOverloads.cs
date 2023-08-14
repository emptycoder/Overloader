﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.Overloads.Utils;
using Overloader.ContentBuilders;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.Overloads;

public sealed class CastTransitionOverloads : IChainMember
{
	ChainAction IChainMember.Execute(GeneratorProperties props, SyntaxNode syntaxNode)
	{
		int formattersCount = props.Store.FormattersIntegrityCount + props.Store.FormattersWoIntegrityCount;
		if (props.Store.OverloadMap is null
		    || !props.Store.IsSmthChanged
		    || props.StartEntry.IgnoreTransitions
		    || formattersCount == 0)
			return ChainAction.NextMember;

		var entry = (MethodDeclarationSyntax) syntaxNode;
		var parameters = entry.ParameterList.Parameters;

		Span<int> maxTransitionsCount = stackalloc int[formattersCount];
		Span<int> transitionIndexes = stackalloc int[formattersCount];
		for (int index = 0, formatterIndex = 0; index < parameters.Count; index++)
		{
			var parameter = parameters[index];
			var mappedParam = props.Store.OverloadMap[index];
			if (mappedParam.ParameterAction is not ParameterAction.FormatterReplacement
			    and not ParameterAction.FormatterIntegrityReplacement)
				continue;

			if (!props.TryGetFormatter(parameter.GetType(props.Compilation).GetClearType(), out var formatter))
				throw new ArgumentException($"Formatter not found for {parameter.Identifier.ToString()}")
					.WithLocation(parameter.GetLocation());
			
			transitionIndexes[formatterIndex] = -1;
			maxTransitionsCount[formatterIndex++] = parameter.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.RefKeyword))
				? 0
				: formatter.Casts.Count;
		}

		// Check that transitions exists
		for (int index = 0;;)
		{
			if (maxTransitionsCount[index] != 0)
			{
				// Skip first, because it was generated by CombinedIntegrityOverload
				transitionIndexes[index] = 0;
				break;
			}
			if (++index == maxTransitionsCount.Length) return ChainAction.NextMember;
		}

		var xmlDocumentation = XmlDocumentation.Parse(entry.GetLeadingTrivia());
		using var bodyBuilder = StringSourceBuilder.Instance;
		using var parameterBuilder = StringSourceBuilder.Instance;
		for (;;)
		{
			bodyBuilder
				.Append(entry.Identifier.ToString())
				.AppendAsConstant("(");
			props.Builder
				.AppendChainMemberNameComment(nameof(CastTransitionOverloads));
			
			parameterBuilder
				.AppendMethodDeclarationSpecifics(entry, props.Store.MethodData)
				.AppendAsConstant("(").WriteTransitionOverload(
					TransitionExtensions.WriteCastTransitionOverload,
					bodyBuilder,
					xmlDocumentation,
					props,
					parameters,
					transitionIndexes,
					(index, formatter) => formatter.Casts[index]);
			
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
				transitionIndexes[index] = -1;

				if (++index == transitionIndexes.Length)
					return ChainAction.NextMember;
			}
		}
	}
}
