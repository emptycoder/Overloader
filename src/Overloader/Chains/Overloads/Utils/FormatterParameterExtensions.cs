using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ContentBuilders;
using Overloader.Entities;
using Overloader.Utils;

namespace Overloader.Chains.Overloads.Utils;

public static class FormatterParameterExtensions
{
	public static ResultOrException<string[]> AppendFormatterParam(
		this SourceBuilder sb,
		GeneratorProperties props,
		byte templateIndex,
		ITypeSymbol type,
		string paramName)
	{
		var rootType = type.GetClearType();
		if (!props.TryGetFormatter(rootType, out var formatter))
			return new ArgumentException(
				$"Can't get formatter for {nameof(type)}: {type.ToDisplayString()}, {nameof(paramName)}: {paramName}.");

		if (!formatter.Params.Any())
			return new ArgumentException(
				$"Params count equals to 0 for {nameof(type)}: {type.ToDisplayString()}, {nameof(paramName)}: {paramName}");

		var @params = rootType.TypeArguments.ToArray();
		if (@params.Length != formatter.GenericParams.Length)
			return new ArgumentException(
				$"Different generic params in formatter ({formatter.GenericParams.Length.ToString()}) and type ({@params.Length.ToString()})");

		string[] decompositionParams = new string[formatter.Params.Length];
		for (int paramIndex = 0;;)
		{
			var formatterParam = formatter.Params[paramIndex];
			var templateType = formatterParam.Param.GetType(props.Templates[templateIndex]);
			props.SetDeepestTypeWithTemplateFilling(templateType, props.Templates[templateIndex])
				.Deconstruct(out var paramType, out var exception);

			if (exception is not null) return exception;
			if (paramType is {IsValueType: true, SpecialType: SpecialType.System_ValueType})
				sb.AppendAsConstant("in")
					.WhiteSpace();

			sb.TrimAppend(paramType.ToDisplayString())
				.WhiteSpace()
				.TrimAppend(paramName)
				.TrimAppend(formatterParam.Identifier);
			decompositionParams[paramIndex] = $"{paramName}{formatterParam.Identifier}";

			if (++paramIndex == formatter.Params.Length) break;
			sb.AppendAsConstant(",")
				.WhiteSpace();
		}

		return decompositionParams;
	}

	public static SourceBuilder AppendIntegrityParam(
		this SourceBuilder sb,
		GeneratorProperties props,
		ParameterData mappedParam,
		ParameterSyntax parameter)
	{
		var template = props.Templates[mappedParam.TemplateIndex];
		var newType = props.SetDeepestType(mappedParam.Type, template, template).PickResult(parameter);
		return sb.AppendAttributes(parameter.AttributeLists, " ")
			.AppendAndBuildModifiers(mappedParam, parameter, newType, " ")
			.TrimAppend(newType.ToDisplayString())
			.WhiteSpace()
			.TrimAppend(parameter.Identifier.ToFullString());
	}
}
