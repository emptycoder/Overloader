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

public sealed class RefIntegrityOverloads : ArrowMethodOverloader, IChainMember
{
	ChainAction IChainMember.Execute(GeneratorProperties props)
	{
		var parameters = props.Store.MethodSyntax.ParameterList.Parameters;
		if (parameters.Count == 0) return ChainAction.NextMember;
		
		Span<int> indexes = stackalloc int[parameters.Count];
		Span<int> maxIndexesCount = stackalloc int[parameters.Count];
		for (int index = 0; index < parameters.Count; index++)
		{
			var parameter = parameters[index];
			indexes[index] = -1;

			if (parameter.Modifiers.Any(modifier =>
				    modifier.IsKind(SyntaxKind.InKeyword)
				    || modifier.IsKind(SyntaxKind.RefKeyword)
				    || modifier.IsKind(SyntaxKind.OutKeyword))) continue;

			foreach (var attributeList in parameter.AttributeLists)
			foreach (var attribute in attributeList.Attributes)
			{
				if (attribute.Name.GetName() is not Ref.TagName) continue;
				maxIndexesCount[index] = 1;
			}
		}
		
		// Check that ref attribute exists
		ushort refCount = 0;
		for (int index = 0; index < indexes.Length; index++)
		{
			if (maxIndexesCount[index] == 0 || refCount++ != 0) continue;
			// Skip first, because it was generated by IntegrityOverload
			indexes[index] = 0;
		}
		
		if (refCount < 2) return ChainAction.NextMember;

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
		int paramIndex)
	{
		head.AppendAsConstant(",")
			.WhiteSpace();
		body.AppendAsConstant(",")
			.WhiteSpace();
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
		var mappedParam = props.Store.MethodData.Parameters[paramIndex];
		var parameter = props.Store.MethodSyntax.ParameterList.Parameters[paramIndex];
		if (indexes[paramIndex] == 0)
			parameter = parameter.WithModifiers(parameter.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.RefKeyword)));

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
			case RequiredReplacement.Formatter:
				head.AppendIntegrityParam(props, mappedParam, parameter);
				break;
			default:
				throw new ArgumentException($"Can't find case for {props.Store.MethodData.Parameters[paramIndex]} parameterAction.")
					.WithLocation(props.Store.MethodSyntax);
		}

		body.AppendParameterWoFormatter(parameter, indexes[paramIndex] == 0 || parameter.Modifiers.Any(SyntaxKind.RefKeyword));
	}

	protected override bool ShiftIndexes(Span<int> indexes, Span<int> maxIndexesCount)
	{
		bool result = base.ShiftIndexes(indexes, maxIndexesCount);
		if (!result) return result;

		// Ignore last overload, because it was generated by MainRefIntegrityOverload
		for (int index = 0; index < indexes.Length; index++)
			if (maxIndexesCount[index] == 1 && indexes[index] != 0)
				return true;
		return false;
	}
}
