using Overloader.Chains.Overloads.Overloads;
using Overloader.ContentBuilders;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Chains.Overloads;

public sealed class DecompositionTransitionOverloads : TransitionDecomposeOverloader, IChainMember
{
	ChainAction IChainMember.Execute(GeneratorProperties props)
	{
		if (props.StartEntry.IsTransitionsIgnored)
			return ChainAction.NextMember;

		var parameters = props.Store.MethodSyntax.ParameterList.Parameters;
		if (parameters.Count == 0) return ChainAction.NextMember;

		Span<int> transitionIndexes = stackalloc int[parameters.Count];
		Span<int> maxTransitionsCount = stackalloc int[parameters.Count];
		for (int index = 0; index < parameters.Count; index++)
		{
			// transitionIndexes[index] = -1;
			var parameter = parameters[index];
			var mappedParam = props.Store.MethodData.Parameters[index];
			if (mappedParam.ReplacementType is not RequiredReplacement.Formatter) continue;
			if (!props.TryGetFormatter(parameter.GetType(props.Compilation).GetClearType(), out var formatter))
				throw new ArgumentException($"Formatter not found for {parameter.Identifier.ToString()}")
					.WithLocation(parameter.GetLocation());
			maxTransitionsCount[index] = formatter.Decompositions.Count;
		}

		// Check that transitions exist
		for (int index = 0;;)
		{
			if (maxTransitionsCount[index] != 0) break;
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
}
