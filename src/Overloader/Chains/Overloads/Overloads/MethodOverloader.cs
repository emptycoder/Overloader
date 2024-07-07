using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Overloader.Chains.Overloads.Utils;
using Overloader.ContentBuilders;
using Overloader.Entities;
using Overloader.Utils;

namespace Overloader.Chains.Overloads.Overloads;

public abstract class MethodOverloader
{
	private readonly string _stageName;
	protected MethodOverloader() => _stageName = GetType().Name;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected void WriteMethodOverload(
		GeneratorProperties props,
		Span<int> indexes = new(),
		Span<int> maxIndexesCount = new())
	{
		using var head = props.Builder.GetDependentInstance();
		using var body = props.Builder.GetDependentInstance();
		head
			.AppendMethodDeclarationSpecifics(props.Store.MethodSyntax, props.Store.MethodDataDto)
			.AppendAsConstant("(");

		var parameters = props.Store.MethodSyntax.ParameterList.Parameters;
		if (parameters.Count != 0)
			for (int paramIndex = 0;;)
			{
				WriteParameter(
					props,
					head,
					body,
					props.Store.XmlDocumentation,
					indexes,
					maxIndexesCount,
					paramIndex);

				if (++paramIndex == parameters.Count) break;
				ParameterSeparatorAppender(props, head, body, paramIndex);
			}

		props.Builder
			.BreakLine()
			.AppendChainMemberNameComment(_stageName)
			.AppendAndClearXmlDocumentation(props.Store.XmlDocumentation)
			.Append(head)
			.AppendAsConstant(")");

		if (props.Store.MethodSyntax.ConstraintClauses.Count > 0)
			props.Builder
				.WhiteSpace()
				.TrimAppend(props.Store.MethodSyntax.ConstraintClauses.ToString());

		if (props.Store.ShouldRemoveBody)
			props.Builder.AppendAsConstant(";");
		else
		{
			WriteMethodBody(props, body);
			props.Builder.AppendAsDependent(body);
		}
	}

	protected void WriteMethodOverloads(
		GeneratorProperties props,
		Span<int> indexes,
		Span<int> maxIndexesCount)
	{
		for (;;)
		{
			WriteMethodOverload(props, indexes, maxIndexesCount);
			if (!ShiftIndexes(indexes, maxIndexesCount)) return;
		}
	}

	protected virtual bool ShiftIndexes(
		Span<int> indexes,
		Span<int> maxIndexesCount)
	{
		/*
		   -1 -1 -1 -1 -1
		   ^
		   
		   0 -1 -1 -1 -1
		   ^
		   Repeat until it < maxLength[index]
		   And when first value equals maxLength[index] reset to zero and add 1 to next rank
		   -1 means that no transition will be applied
		   -1 0 -1 -1 -1
		   ^
		   
		   0 0 -1 -1 -1
		   ^
		   And so on...
		*/
		for (int index = 0;;)
		{
			if (indexes[index] != maxIndexesCount[index]
			    && ++indexes[index] != maxIndexesCount[index]) break;
			indexes[index] = -1;

			if (++index == indexes.Length)
				return false;
		}

		return true;
	}

	protected abstract void WriteMethodBody(
		GeneratorProperties props,
		SourceBuilder body);

	protected abstract void WriteParameter(
		GeneratorProperties props,
		SourceBuilder head,
		SourceBuilder body,
		XmlDocumentation xmlDocumentation,
		Span<int> indexes,
		Span<int> maxIndexesCount,
		int paramIndex);

	protected abstract void ParameterSeparatorAppender(
		GeneratorProperties props,
		SourceBuilder head,
		SourceBuilder body,
		int paramIndex);
}
