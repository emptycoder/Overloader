using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ContentBuilders;
using Overloader.Entities;
using Overloader.Enums;

namespace Overloader.Chains.Overloads.Utils;

public static class MethodBodyExtensions
{
	public static void AppendCombined(
		this SourceBuilder body,
		GeneratorProperties props,
		int paramIndex)
	{
		var mappedParam = props.Store.OverloadMap[paramIndex];
		var parameter = props.Store.MethodSyntax.ParameterList.Parameters[paramIndex];
		switch (mappedParam.ReplacementType)
		{
			case ParameterReplacement.Formatter:
			{
				string[] decompositionParams = EmptySourceBuilder.Instance
					.AppendFormatterParam(
						props,
						mappedParam.Type,
						parameter.Identifier.ValueText)
					.PickResult(parameter);
				body.TrimAppend(string.Join(", ", decompositionParams));
				break;
			}
			case ParameterReplacement.None:
			case ParameterReplacement.Template:
			case ParameterReplacement.UserType:
			case ParameterReplacement.FormatterIntegrity:
			default:
				body.AppendCombinedWoFormatter(mappedParam, parameter);
				break;
		}
	}

	public static void AppendCombinedWoFormatter(
		this SourceBuilder body,
		ParameterData mappedParam,
		ParameterSyntax parameter)
	{
		if (mappedParam.Type.IsRefLikeType)
			body
				.AppendAsConstant("ref")
				.WhiteSpace();
		body.TrimAppend(parameter.Identifier.ValueText);
	}

	public static void AppendVariableToBody(
		this SourceBuilder bodyBuilder,
		ParameterSyntax parameter,
		string? paramName = null)
	{
		if (parameter.Modifiers.Any(SyntaxKind.RefKeyword))
			bodyBuilder
				.AppendAsConstant("ref")
				.WhiteSpace();
		bodyBuilder.TrimAppend(paramName ?? parameter.Identifier.ValueText);
	}
}
