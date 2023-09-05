﻿using Overloader.Chains.Overloads.Overloads;
using Overloader.ContentBuilders;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Chains.Overloads;

public sealed class CombinedDecompositionTransitionOverloads : TransitionDecomposeOverloader, IChainMember
{
	ChainAction IChainMember.Execute(GeneratorProperties props)
	{
		if (props.Store.OverloadMap is null
		    || !props.Store.IsSmthChanged
		    || props.StartEntry.IgnoreTransitions)
			return ChainAction.NextMember;
		
		var parameters = props.Store.MethodSyntax.ParameterList.Parameters;
		
		if (parameters.Count == 0) return ChainAction.NextMember;

		ushort countOfCombineWith = 0;
		Span<int> transitionsIndexes = stackalloc int[parameters.Count];
		Span<int> maxTransitionsCount = stackalloc int[parameters.Count];
		for (int index = 0; index < parameters.Count; index++)
		{
			// transitionsIndexes[index] = -1;
			var parameter = parameters[index];
			var mappedParam = props.Store.OverloadMap![index];
			if (mappedParam.ReplacementType is not ParameterReplacement.Formatter) continue;
			if (!mappedParam.IsCombineNotExists)
			{
				countOfCombineWith++;
				continue;
			}

			if (!props.TryGetFormatter(parameter.GetType(props.Compilation).GetClearType(), out var formatter))
				throw new ArgumentException($"Formatter not found for {parameter.Identifier.ToString()}")
					.WithLocation(parameter.GetLocation());
			maxTransitionsCount[index] = formatter.Decompositions.Count;
		}
		
		if (countOfCombineWith == 0) return ChainAction.NextMember;
		
		// Check that transitions exists
		for (int index = 0;;)
		{
			if (maxTransitionsCount[index] != 0) break;
			if (++index == maxTransitionsCount.Length) return ChainAction.NextMember;
		}
		
		WriteMethodOverloads(
			props,
			XmlDocumentation.Parse(props.Store.MethodSyntax.GetLeadingTrivia()),
			transitionsIndexes,
			maxTransitionsCount);

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
		if (mappedParam.IsCombineNotExists)
			base.WriteParameter(
				props,
				head,
				body,
				xmlDocumentation,
				indexes,
				paramIndex);
		else
			base.WriteParameter(
				props,
				EmptySourceBuilder.Instance,
				body,
				XmlDocumentation.Empty,
				indexes,
				mappedParam.CombineIndex);
	}
}