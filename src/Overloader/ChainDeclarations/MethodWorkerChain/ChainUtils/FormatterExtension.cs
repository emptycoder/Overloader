﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ContentBuilders;
using Overloader.Models;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain.ChainUtils;

public static class FormatterExtension
{
	public static string AppendFormatterParam(this SourceBuilder sb,
		GeneratorProperties props,
		ITypeSymbol type,
		string paramName)
	{
		var rootType = type.GetClearType();
		if (!props.TryGetFormatter(rootType, out var formatter))
			throw new ArgumentException(
				$"Can't get formatter for {nameof(type)}: {type.ToDisplayString()}, {nameof(paramName)}: {paramName}.");

		if (!formatter.Params.Any())
			throw new ArgumentException(
				$"Params count equals to 0 for {nameof(type)}: {type.ToDisplayString()}, {nameof(paramName)}: {paramName}");

		var @params = rootType.TypeArguments.ToArray();
		if (@params.Length != formatter.GenericParams.Length)
			throw new ArgumentException(
				$"Different generic params in formatter ({formatter.GenericParams.Length}) and type ({@params.Length})");

		using var bodyBuilder = SourceBuilder.GetInstance();
		for (int paramIndex = 0;;)
		{
			var formatterParam = formatter.Params[paramIndex];
			var templateType = formatterParam.Param.GetType(props.Template);
			var paramType = props.SetDeepestTypeWithTemplateFilling(templateType, props.Template);

			if (paramType is {IsValueType: true, SpecialType: SpecialType.System_ValueType})
				sb.AppendWith("in", " ");

			sb.AppendWith(paramType.ToDisplayString(), " ")
				.Append(paramName)
				.Append(formatterParam.Identifier);
			bodyBuilder.AppendWoTrim(paramName).AppendWoTrim(formatterParam.Identifier);

			if (++paramIndex == formatter.Params.Length) break;
			sb.AppendWoTrim(", ");
			bodyBuilder.AppendWoTrim(", ");
		}

		return bodyBuilder.ToString();
	}

	public static SourceBuilder AppendIntegrityParam(
		this SourceBuilder sb,
		GeneratorProperties props,
		ParameterData mappedParam,
		ParameterSyntax parameter)
	{
		var newType = props.SetDeepestType(mappedParam.Type, props.Template, props.Template);

		return sb.AppendAttributes(parameter.AttributeLists, " ")
			.AppendWoTrim(mappedParam.BuildModifiersWithWhitespace(parameter, newType))
			.AppendWith(newType.ToDisplayString(), " ")
			.Append(parameter.Identifier.ToFullString());
	}
}
