using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ContentBuilders;
using Overloader.Models;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.Overloads.Utils;

public static class FormatterExtension
{
	public static ResultOrException<string> AppendFormatterParam(this SourceBuilder sb,
		GeneratorProperties props,
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
				$"Different generic params in formatter ({formatter.GenericParams.Length}) and type ({@params.Length})");

		using var bodyBuilder = SourceBuilder.GetInstance();
		for (int paramIndex = 0;;)
		{
			var formatterParam = formatter.Params[paramIndex];
			var templateType = formatterParam.Param.GetType(props.Template);
			props.SetDeepestTypeWithTemplateFilling(templateType, props.Template)
				.Deconstruct(out var paramType, out var exception);
			
			if (exception is not null) return exception;
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
		var newType = props.SetDeepestType(mappedParam.Type, props.Template, props.Template).PickResult(parameter);
		return sb.AppendAttributes(parameter.AttributeLists, " ")
			.AppendWoTrim(mappedParam.BuildModifiers(parameter, newType, " "))
			.AppendWith(newType.ToDisplayString(), " ")
			.Append(parameter.Identifier.ToFullString());
	}
}
