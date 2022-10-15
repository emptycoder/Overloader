using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Entities.Builders;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain.ChainUtils;

internal static class FormatterExtension
{
	public static string AppendFormatterParam(this SourceBuilder sb,
		GeneratorProperties props,
		SyntaxTokenList modifiers,
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
			var templateType = formatterParam.Param.GetType(props.Template) ??
			                   type.GetMemberType(formatterParam.Name);
			var paramType = props.SetDeepestTypeWithTemplateFilling(templateType);
			if (paramType.IsValueType && !paramType.IsDefinition)
				sb.AppendWith("in", " ");

			sb.AppendWith(paramType.ToDisplayString(), " ")
				.Append(paramName)
				.Append(formatterParam.Name);
			bodyBuilder.AppendWoTrim(paramName).AppendWoTrim(formatterParam.Name);

			if (++paramIndex == formatter.Params.Length) break;
			sb.AppendWoTrim(", ");
			bodyBuilder.AppendWoTrim(", ");
		}

		return bodyBuilder.ToString();
	}

	public static SourceBuilder AppendIntegrityParam(this SourceBuilder sb, GeneratorProperties props, ITypeSymbol type, ParameterSyntax parameter) =>
		sb.AppendWith(parameter.AttributeLists.ToFullString(), " ")
			.AppendWith(parameter.Modifiers.ToFullString(), " ")
			.AppendWith(props.SetDeepestType(type, props.Template ?? type).ToDisplayString(), " ")
			.Append(parameter.Identifier.ToFullString());
}
