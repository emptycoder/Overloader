using Microsoft.CodeAnalysis;
using Overloader.Entities;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain.ChainUtils;

internal static class TypeExtensions
{
	public static ITypeSymbol SetDeepestType(this GeneratorProperties props, ITypeSymbol argType, ITypeSymbol templateType)
	{
		var rootType = argType.GetClearType();
		if (!rootType.IsGenericType || !props.TryGetFormatter(rootType, out var formatter)) return templateType;

		var @params = rootType.TypeArguments.ToArray();
		if (@params.Length != formatter.GenericParams.Length)
			throw new ArgumentException(
				$"Different generic params in formatter ({formatter.GenericParams.Length}) and type ({@params.Length})");

		for (int paramIndex = 0; paramIndex < formatter.GenericParams.Length; paramIndex++)
		{
			var genericType = formatter.GenericParams[paramIndex].GetType(props.Template);
			if (genericType is null) return templateType;
			@params[paramIndex] = props.SetDeepestType(@params[paramIndex], genericType);
		}

		return argType.ConstructWithClearType(rootType.OriginalDefinition.Construct(@params), props.Compilation);
	}

	public static ITypeSymbol SetDeepestTypeWithTemplateFilling(this GeneratorProperties props, ITypeSymbol templateType)
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
