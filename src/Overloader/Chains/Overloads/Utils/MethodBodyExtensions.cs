using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ContentBuilders;
using Overloader.Entities;
using Overloader.Enums;

namespace Overloader.Chains.Overloads.Utils;

public static class MethodBodyExtensions
{
	public static void AppendCombinedParameter(
		this SourceBuilder body,
		GeneratorProperties props,
		int paramIndex)
	{
		var mappedParam = props.Store.OverloadMap[paramIndex];
		var parameter = props.Store.MethodSyntax.ParameterList.Parameters[paramIndex];
		switch (mappedParam.ReplacementType)
		{
			case RequiredReplacement.Formatter:
			{
				string[] decompositionParams = EmptySourceBuilder.Instance
					.AppendFormatterParam(
						props,
						mappedParam.TemplateIndex,
						mappedParam.Type,
						parameter.Identifier.ValueText)
					.PickResult(parameter);
				body.TrimAppend(string.Join(", ", decompositionParams));
				break;
			}
			case RequiredReplacement.None:
			case RequiredReplacement.Template:
			case RequiredReplacement.UserType:
			case RequiredReplacement.FormatterIntegrity:
			default:
				body.AppendParameterWoFormatter(parameter);
				break;
		}
	}

	public static void AppendParameterWoFormatter(
		this SourceBuilder body,
		ParameterSyntax parameter,
		bool? isRef = null,
		string? paramName = null)
	{
		if (isRef ?? parameter.Modifiers.Any(SyntaxKind.RefKeyword))
			body
				.AppendAsConstant("ref")
				.WhiteSpace();
		body.TrimAppend(paramName ?? parameter.Identifier.ValueText);
	}
}
