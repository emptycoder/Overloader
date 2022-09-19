using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Entities.Formatters.Params;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain.Utils;

internal static class FormatterExtension
{
	public static unsafe string AppendFormatter(this GeneratorProperties props,
		ITypeSymbol type,
		string paramName)
	{
		var rootType = type.GetRootType();
		if (!props.TryGetFormatter(rootType, out var formatter))
			throw new ArgumentException(
				$"Can't get formatter for {nameof(type)}: {type.ToDisplayString()}, {nameof(paramName)}: {paramName}.");

		if (formatter.Params.Length == 0)
			throw new ArgumentException(
				$"Params count equals to 0 for {nameof(type)}: {type.ToDisplayString()}, {nameof(paramName)}: {paramName}");

		var @params = rootType.TypeArguments.ToArray();
		if (@params.Length != formatter.GenericParams.Length)
			throw new ArgumentException(
				$"Different generic params in formatter ({formatter.GenericParams.Length}) and type ({@params.Length})");

		int paramIndex = 0;
		int charLengthOfParams = AppendFormatterParam();
		const string paramsSeparator = ", ";
		for (paramIndex = 1; paramIndex < formatter.Params.Length; paramIndex++)
		{
			props.Builder.AppendWoTrim(paramsSeparator);
			charLengthOfParams += AppendFormatterParam();
		}

		// To avoid additional heapAllocation from StringBuilder
		int bufferLen = charLengthOfParams +
		                (paramsSeparator.Length * (formatter.Params.Length - 1));
		char* strBuffer = stackalloc char[bufferLen];
		int index = 0;
		paramIndex = 0;
		PushToBuffer(paramName);
		PushToBuffer(formatter.Params[paramIndex].Name!);
		for (paramIndex = 1; paramIndex < formatter.Params.Length; paramIndex++)
		{
			PushToBuffer(paramsSeparator);
			PushToBuffer(paramName);
			PushToBuffer(formatter.Params[paramIndex].Name!);
		}

		return new string(strBuffer, 0, bufferLen);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void PushToBuffer(string str)
		{
			// ReSharper disable once ForCanBeConvertedToForeach
			for (int strIndex = 0; strIndex < str.Length; strIndex++)
				strBuffer[index++] = str[strIndex];
		}

		int AppendFormatterParam()
		{
			// ReSharper disable once SuggestVarOrType_SimpleTypes
			// Broken var type specified as IParam?
			IParam formatterParam = formatter.Params[paramIndex];
			props.Builder.AppendWith(GetDeeperType().ToDisplayString(), " ")
				.Append(paramName)
				.Append(formatterParam.Name);
			return paramName.Length + formatterParam.Name!.Length;

			ITypeSymbol GetDeeperType()
			{
				var paramType = formatterParam.GetType(props.Template) ??
				                type.GetMemberType(formatterParam.Name!);
				paramType = props.GoDeeper(paramType, paramType);
				var paramRootType = paramType.GetRootType();
				if (!paramRootType.IsUnboundGenericType) return paramType;

				var typeParameters = new ITypeSymbol[paramRootType.TypeParameters.Length];
				for (int typeParamIndex = 0; typeParamIndex < typeParameters.Length; typeParamIndex++)
					typeParameters[typeParamIndex] = props.Template ?? throw new Exception(
						"Unexpected declaration of unbound generic in method.");

				return paramType.SetRootType(paramRootType.OriginalDefinition.Construct(typeParameters), props.Compilation);
			}
		}
	}

	public static void AppendFormatterIntegrity(this GeneratorProperties props, ITypeSymbol type, ParameterSyntax parameterSyntax) =>
		props.Builder.AppendWith(parameterSyntax.Modifiers.ToFullString(), " ")
			.AppendWith(props.GoDeeper(type, props.Template ?? type).ToDisplayString(), " ")
			.Append(parameterSyntax.Identifier.ToFullString());

	private static ITypeSymbol GoDeeper(this GeneratorProperties gsb, ITypeSymbol argType, ITypeSymbol paramType)
	{
		var rootType = argType.GetRootType();
		if (!rootType.IsGenericType || !gsb.TryGetFormatter(rootType, out var formatter)) return paramType;

		var @params = rootType.TypeArguments.ToArray();
		if (@params.Length != formatter.GenericParams.Length)
			throw new ArgumentException(
				$"Different generic params in formatter ({formatter.GenericParams.Length}) and type ({@params.Length})");

		for (int paramIndex = 0; paramIndex < formatter.GenericParams.Length; paramIndex++)
		{
			var genericType = formatter.GenericParams[paramIndex].GetType(gsb.Template);
			if (genericType is null) return paramType;
			@params[paramIndex] = gsb.GoDeeper(@params[paramIndex], genericType);
		}

		return argType.SetRootType(rootType.OriginalDefinition.Construct(@params), gsb.Compilation);
	}
}
