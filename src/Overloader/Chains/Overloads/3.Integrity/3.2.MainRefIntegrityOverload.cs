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

public sealed class MainRefIntegrityOverload : BodyMethodsOverloader, IChainMember
{
	ChainAction IChainMember.Execute(GeneratorProperties props)
	{
		var parameters = props.Store.MethodSyntax.ParameterList.Parameters;
		if (parameters.Count == 0) return ChainAction.NextMember;

		Span<int> indexes = stackalloc int[parameters.Count];
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
				indexes[index] = 1;
			}
		}

		// Check that ref attribute exists
		for (int index = 0;;)
		{
			if (indexes[index] == 1) break;
			if (++index == indexes.Length) return ChainAction.NextMember;
		}

		WriteMethodOverload(props, indexes);
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
		SourceBuilder body) =>
		WriteMethodBody(props, body, Array.Empty<(string, string)>());

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
		if (indexes[paramIndex] == 1)
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
				throw new ArgumentException($"Can't find case for {props.Store.OverloadMap[paramIndex]} parameterAction.")
					.Unreachable()
					.WithLocation(props.Store.MethodSyntax);
		}
	}
}
