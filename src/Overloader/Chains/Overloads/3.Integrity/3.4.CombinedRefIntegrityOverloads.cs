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

public sealed class CombinedRefIntegrityOverloads : ArrowMethodOverloader, IChainMember
{
	ChainAction IChainMember.Execute(GeneratorProperties props)
	{
		if (props.Store.CombineParametersCount == 0)
			return ChainAction.NextMember;

		var parameters = props.Store.MethodSyntax.ParameterList.Parameters;
		if (parameters.Count == 0) return ChainAction.NextMember;

		Span<int> maxIndexesCount = stackalloc int[parameters.Count];
		Span<int> indexes = stackalloc int[parameters.Count];
		for (int index = 0; index < parameters.Count; index++)
		{
			var parameter = parameters[index];
			indexes[index] = -1;

			if (!props.Store.MethodDataDto.Parameters[index].IsCombineNotExists
			    || parameter.Modifiers.Any(modifier =>
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
		for (int index = 0;;)
		{
			if (maxIndexesCount[index] != 0)
			{
				// Skip first, because it was generated by IntegrityOverload
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
		int paramIndex)
	{
		var mappedParam = props.Store.MethodDataDto.Parameters[paramIndex];
		if (mappedParam.IsCombineNotExists)
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
		var mappedParam = props.Store.MethodDataDto.Parameters[paramIndex];
		var parameters = props.Store.MethodSyntax.ParameterList.Parameters;
		var parameter = parameters[paramIndex];

		string paramName = parameter.Identifier.ToString();
		if (indexes[paramIndex] == 0)
			parameter = parameter.WithModifiers(parameter.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.RefKeyword)));

		if (mappedParam.IsCombineNotExists)
		{
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
					throw new ArgumentException($"Can't find case for {props.Store.MethodDataDto.Parameters[paramIndex]} parameterAction.")
						.WithLocation(props.Store.MethodSyntax);
			}

			body.AppendParameterWoFormatter(
				parameter,
				maxIndexesCount[paramIndex] == 1 || parameter.Modifiers.Any(SyntaxKind.RefKeyword),
				paramName);
		}
		else
		{
			parameter = parameters[mappedParam.CombineIndex];
			body.AppendParameterWoFormatter(
				parameter,
				maxIndexesCount[paramIndex] == 1 || parameter.Modifiers.Any(SyntaxKind.RefKeyword));
		}
	}
}
