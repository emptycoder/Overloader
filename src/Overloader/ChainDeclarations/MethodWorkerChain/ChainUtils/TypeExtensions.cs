using Microsoft.CodeAnalysis;
using Overloader.Models;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain.ChainUtils;

internal static class TypeExtensions
{
	public static ITypeSymbol SetDeepestType(this GeneratorProperties props,
		ITypeSymbol argType,
		ITypeSymbol templateType,
		ITypeSymbol exceptionType)
	{
		var clearType = argType.GetClearType();
		if (!clearType.IsGenericType || !props.TryGetFormatter(clearType, out var formatter)) return exceptionType;

		var @params = clearType.TypeArguments.ToArray();
		if (@params.Length != formatter.GenericParams.Length)
			throw new ArgumentException(
				$"Different generic params in formatter ({formatter.GenericParams.Length}) and type ({@params.Length})");

		for (int paramIndex = 0; paramIndex < formatter.GenericParams.Length; paramIndex++)
		{
			var genericType = formatter.GenericParams[paramIndex].GetType(templateType);
			@params[paramIndex] = props.SetDeepestType(@params[paramIndex], templateType, genericType);
		}

		return argType.ConstructWithClearType(clearType.OriginalDefinition.Construct(@params), props.Compilation);
	}

	public static ITypeSymbol SetDeepestTypeWithTemplateFilling(this GeneratorProperties props,
		ITypeSymbol argType,
		ITypeSymbol templateType)
	{
		var paramType = props.SetDeepestType(argType, templateType, argType);
		var paramClearType = paramType.GetClearType();
		if (!paramClearType.IsUnboundGenericType) return paramType;

		// Auto fill unbound generic by template
		var typeParameters = new ITypeSymbol[paramClearType.TypeParameters.Length];
		for (int typeParamIndex = 0; typeParamIndex < typeParameters.Length; typeParamIndex++)
			typeParameters[typeParamIndex] = templateType ?? throw new Exception(
				"Unexpected declaration of unbound generic in method.");

		return paramType.ConstructWithClearType(paramClearType.OriginalDefinition.Construct(typeParameters), props.Compilation);
	}
}
