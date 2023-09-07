using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Overloader.Chains.Overloads.Overloads;
using Overloader.Chains.Overloads.Utils;
using Overloader.ContentBuilders;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Chains.Overloads;

public sealed class DecompositionOverloads : BodyMethodsOverloader, IChainMember
{
	private readonly AsyncLocal<List<(string, string)>> _replacements = new();

	ChainAction IChainMember.Execute(GeneratorProperties props)
	{
		if (props.StartEntry.IgnoreTransitions)
			return ChainAction.NextMember;

		var parameters = props.Store.MethodSyntax.ParameterList.Parameters;
		if (parameters.Count == 0) return ChainAction.NextMember;

		Span<int> indexes = stackalloc int[parameters.Count];
		Span<int> maxIndexesCount = stackalloc int[parameters.Count];
		for (int index = 0; index < parameters.Count; index++)
		{
			indexes[index] = -1;
			var parameter = parameters[index];
			var mappedParam = props.Store.OverloadMap[index];
			if (mappedParam.ReplacementType is not RequiredReplacement.Formatter) continue;
			if (!props.TryGetFormatter(parameter.GetType(props.Compilation).GetClearType(), out _))
				throw new ArgumentException($"Formatter not found for {parameter.Identifier.ToString()}")
					.WithLocation(parameter.GetLocation());

			maxIndexesCount[index] = parameter.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.RefKeyword))
				? 0
				: 1;
		}

		// Check that transitions exists
		for (int index = 0;;)
		{
			if (maxIndexesCount[index] != 0)
			{
				// Skip first, because it was generated by CombinedIntegrityOverload
				indexes[index] = 0;
				break;
			}

			if (++index == maxIndexesCount.Length) return ChainAction.NextMember;
		}

		WriteMethodOverloads(
			props,
			indexes,
			maxIndexesCount);

		return ChainAction.NextMember;
	}

	protected override void ParameterSeparatorAppender(
		GeneratorProperties props,
		SourceBuilder head,
		SourceBuilder body,
		int paramIndex) =>
		head.AppendAsConstant(",")
			.WhiteSpace();

	protected override void WriteMethodBody(
		GeneratorProperties props,
		SourceBuilder body)
	{
		var list = _replacements.Value;
		WriteMethodBody(props, body, list.ToArray());
		list.Clear();
	}

	protected override void WriteParameter(
		GeneratorProperties props,
		SourceBuilder head,
		SourceBuilder body,
		XmlDocumentation xmlDocumentation,
		Span<int> indexes,
		Span<int> maxIndexesCount,
		int paramIndex)
	{
		var mappedParam = props.Store.OverloadMap[paramIndex];
		var parameter = props.Store.MethodSyntax.ParameterList.Parameters[paramIndex];
		string paramName = parameter.Identifier.ToString();
		switch (mappedParam.ReplacementType)
		{
			case RequiredReplacement.None:
				head.TrimAppend(parameter.ToFullString());
				break;
			case RequiredReplacement.Template:
			case RequiredReplacement.UserType:
				head.AppendParameter(parameter, mappedParam, props.Compilation);
				break;
			case RequiredReplacement.FormatterIntegrity:
				head.AppendIntegrityParam(props, mappedParam, parameter);
				break;
			case RequiredReplacement.Formatter when indexes[paramIndex] == -1:
				head.AppendIntegrityParam(props, mappedParam, parameter);
				break;
			case RequiredReplacement.Formatter:
				string[] decompositionParams = head
					.AppendFormatterParam(
						props,
						mappedParam.Type,
						paramName)
					.PickResult(parameter);

				var list = _replacements.Value ?? (_replacements.Value = new List<(string, string)>());
				list.Add((paramName, string.Join(", ", decompositionParams)));
				foreach (string decompositionParam in decompositionParams)
					xmlDocumentation.AddOverload(paramName, decompositionParam);
				return;
			default:
				throw new ArgumentException($"Can't find case for '{mappedParam.ReplacementType}' parameter action.")
					.Unreachable()
					.WithLocation(parameter);
		}

		xmlDocumentation.AddOverload(paramName, paramName);
	}
}
