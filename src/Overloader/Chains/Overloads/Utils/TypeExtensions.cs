using Microsoft.CodeAnalysis;
using Overloader.Entities;
using Overloader.Utils;

namespace Overloader.Chains.Overloads.Utils;

public static class TypeExtensions
{
	public static ResultOrException<ITypeSymbol> SetDeepestType(
		this GeneratorProperties props,
		ITypeSymbol argType,
		ITypeSymbol templateType,
		ITypeSymbol exceptionType)
	{
		var clearType = argType.GetClearType();
		if (!clearType.IsGenericType) return ResultOrException<ITypeSymbol>.Result(exceptionType);
		if (!props.TryGetFormatter(clearType, out var formatter))
			return new ArgumentException($"Not found formatter for {argType}.");

		var @params = clearType.TypeArguments.ToArray();
		if (@params.Length != formatter.GenericParams.Length)
			return new ArgumentException(
				$"Different generic params in formatter ({formatter.GenericParams.Length}) and type ({@params.Length})");

		for (int paramIndex = 0; paramIndex < formatter.GenericParams.Length; paramIndex++)
		{
			var genericType = formatter.GenericParams[paramIndex].GetType(templateType);
			props.SetDeepestType(@params[paramIndex], templateType, genericType)
				.Deconstruct(out var value, out var exception);
			if (exception is not null) return exception;

			@params[paramIndex] = value;
		}

		return ResultOrException<ITypeSymbol>.Result(argType.ConstructWithClearType(clearType.OriginalDefinition.Construct(@params), props.Compilation));
	}

	public static ResultOrException<ITypeSymbol> SetDeepestTypeWithTemplateFilling(
		this GeneratorProperties props,
		ITypeSymbol argType,
		ITypeSymbol? templateType)
	{
		if (templateType is null)
			return new ArgumentException("Unexpected declaration of unbound generic in method.");

		props.SetDeepestType(argType, templateType, argType)
			.Deconstruct(out var paramType, out var exception);
		if (exception is not null) return exception;

		var paramClearType = paramType.GetClearType();
		if (!paramClearType.IsUnboundGenericType) return ResultOrException<ITypeSymbol>.Result(paramType);

		// Auto fill unbound generic by template
		var typeParameters = new ITypeSymbol[paramClearType.TypeParameters.Length];
		for (int typeParamIndex = 0; typeParamIndex < typeParameters.Length; typeParamIndex++)
			typeParameters[typeParamIndex] = templateType;

		return ResultOrException<ITypeSymbol>.Result(paramType.ConstructWithClearType(paramClearType.OriginalDefinition.Construct(typeParameters),
			props.Compilation));
	}
}
