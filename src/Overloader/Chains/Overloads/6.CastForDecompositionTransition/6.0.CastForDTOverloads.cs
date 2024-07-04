using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Overloader.Chains.Overloads.Overloads;
using Overloader.ContentBuilders;
using Overloader.Entities;
using Overloader.Entities.Formatters;
using Overloader.Entities.Formatters.Transitions;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Chains.Overloads;

// ReSharper disable once InconsistentNaming
public sealed class CastForDTOverloads : TransitionCastOverloader, IChainMember
{
	ChainAction IChainMember.Execute(GeneratorProperties props)
	{
		if (props.StartEntry.IsTransitionsIgnored)
			return ChainAction.NextMember;

		var parameters = props.Store.MethodSyntax.ParameterList.Parameters;
		if (parameters.Count == 0) return ChainAction.NextMember;

		Span<int> maxTransitionsCount = stackalloc int[parameters.Count];
		Span<int> transitionIndexes = stackalloc int[parameters.Count];
		for (int index = 0; index < parameters.Count; index++)
		{
			transitionIndexes[index] = -1;
			var parameter = parameters[index];
			var mappedParam = props.Store.MethodData.Parameters[index];
			if (mappedParam.ReplacementType is not RequiredReplacement.Formatter) continue;
			if (!props.TryGetFormatter(parameter.GetType(props.Compilation).GetClearType(), out var formatter))
				throw new ArgumentException($"Formatter not found for {parameter.Identifier.ToString()}")
					.WithLocation(parameter.GetLocation());

			maxTransitionsCount[index] = parameter.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.RefKeyword))
				? 0
				: formatter.CastsForDecomposition.Count;
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

		WriteMethodOverloads(
			props,
			transitionIndexes,
			maxTransitionsCount);

		return ChainAction.NextMember;
	}

	protected override void ParameterSeparatorAppender(
		GeneratorProperties props,
		SourceBuilder head,
		SourceBuilder body,
		int paramIndex)
	{
		head.AppendAsConstant(",")
			.WhiteSpace();
		body.AppendAsConstant(",")
			.WhiteSpace();
	}

	protected override CastModel GetCastModel(
		FormatterModel formatter,
		int transitionIndex) =>
		formatter.CastsForDecomposition[transitionIndex];
}
