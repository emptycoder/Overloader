using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain.Utils;

internal static class FormatterExtension
{
	public static string AppendFormatterParam(this GeneratorProperties props, ITypeSymbol type, string paramName)
	{
		var rootType = type.GetClearType();
		if (!props.TryGetFormatter(rootType, out var formatter)) throw new ArgumentException(
				$"Can't get formatter for {nameof(type)}: {type.ToDisplayString()}, {nameof(paramName)}: {paramName}.");
		
		if (formatter.Params.Length == 0) throw new ArgumentException(
				$"Params count equals to 0 for {nameof(type)}: {type.ToDisplayString()}, {nameof(paramName)}: {paramName}");

		var @params = rootType.TypeArguments.ToArray();
		if (@params.Length != formatter.GenericParams.Length) throw new ArgumentException(
				$"Different generic params in formatter ({formatter.GenericParams.Length}) and type ({@params.Length})");

		// TODO: use "in" keyword for type when it's needed
		using var sb = SourceBuilder.GetInstance();
		for (int paramIndex = 0;;)
		{
			var formatterParam = formatter.Params[paramIndex];
			var templateType = formatterParam.Param.GetType(props.Template) ??
			                   type.GetMemberType(formatterParam.Name);
			props.Builder.AppendWith(props.SetDeepestTypeWithTemplateFilling(templateType).ToDisplayString(), " ")
				.Append(paramName)
				.Append(formatterParam.Name);
			sb.AppendWoTrim(paramName).AppendWoTrim(formatterParam.Name);
			
			if (++paramIndex == formatter.Params.Length) break;
			props.Builder.AppendWoTrim(", ");
			sb.AppendWoTrim(", ");
		}

		return sb.ToString();
	}

	public static SourceBuilder AppendIntegrityParam(this SourceBuilder sb, GeneratorProperties props, ITypeSymbol type, ParameterSyntax parameter) =>
		sb.AppendWith(parameter.AttributeLists.ToFullString(), " ")
			.AppendWith(parameter.Modifiers.ToFullString(), " ")
			.AppendWith(props.SetDeepestType(type, props.Template ?? type).ToDisplayString(), " ")
			.Append(parameter.Identifier.ToFullString());

	private static ITypeSymbol SetDeepestType(this GeneratorProperties props, ITypeSymbol argType, ITypeSymbol templateType)
	{
		var rootType = argType.GetClearType();
		if (!rootType.IsGenericType || !props.TryGetFormatter(rootType, out var formatter)) return templateType;

		var @params = rootType.TypeArguments.ToArray();
		if (@params.Length != formatter.GenericParams.Length) throw new ArgumentException(
				$"Different generic params in formatter ({formatter.GenericParams.Length}) and type ({@params.Length})");

		for (int paramIndex = 0; paramIndex < formatter.GenericParams.Length; paramIndex++)
		{
			var genericType = formatter.GenericParams[paramIndex].GetType(props.Template);
			if (genericType is null) return templateType;
			@params[paramIndex] = props.SetDeepestType(@params[paramIndex], genericType);
		}

		return argType.ConstructWithClearType(rootType.OriginalDefinition.Construct(@params), props.Compilation);
	}
	
	private static ITypeSymbol SetDeepestTypeWithTemplateFilling(this GeneratorProperties props, ITypeSymbol templateType)
	{
		var paramType = props.SetDeepestType(templateType, templateType);
		var paramClearType = paramType.GetClearType();
		if (!paramClearType.IsUnboundGenericType) return paramType;

		// Auto fill unbound generic by template
		var typeParameters = new ITypeSymbol[paramClearType.TypeParameters.Length];
		for (int typeParamIndex = 0; typeParamIndex < typeParameters.Length; typeParamIndex++)
			typeParameters[typeParamIndex] = props.Template ?? throw new Exception(
				"Unexpected declaration of unbound generic in method.");

		return paramType.ConstructWithClearType(paramClearType.OriginalDefinition.Construct(typeParameters), props.Compilation);
	}
}
