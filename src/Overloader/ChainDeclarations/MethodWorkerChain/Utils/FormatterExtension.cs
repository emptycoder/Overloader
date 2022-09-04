using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain.Utils;

internal static class FormatterExtension
{
	public static unsafe string AppendFormatter(this GeneratorSourceBuilder gsb,
		ITypeSymbol type,
		string paramName)
	{
		if (!gsb.TryGetFormatter(type, out var formatter))
			throw new ArgumentException(
				$"Can't get formatter for {nameof(type)}: {type.ToDisplayString()}, {nameof(paramName)}: {paramName}.");

		if (formatter.Params.Length == 0)
			throw new ArgumentException(
				$"Params count equals to 0 for {nameof(type)}: {type.ToDisplayString()}, {nameof(paramName)}: {paramName}");
		int paramIndex = 0;
		int charLengthOfParams = AppendFormatterParam();
		const string paramsSeparator = ", ";
		for (paramIndex = 1; paramIndex < formatter.Params.Length; paramIndex++)
		{
			gsb.AppendWoTrim(paramsSeparator);
			charLengthOfParams += AppendFormatterParam();
		}

		// To avoid double heapAllocation when using StreamBuilder
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
			var formatterParam = formatter.Params[paramIndex];
			gsb.AppendWith((formatterParam.GetType(gsb.Template) ??
			                type.GetMemberType(formatterParam.Name!)).ToDisplayString(), " ")
				.Append(paramName)
				.Append(formatterParam.Name);
			return paramName.Length + formatterParam.Name!.Length;
		}
	}

	public static void AppendFormatterIntegrity(this GeneratorSourceBuilder gsb, ITypeSymbol type, ParameterSyntax parameterSyntax)
	{
		if (!gsb.TryGetFormatter(type, out var formatter))
			throw new ArgumentException($"Can't get formatter by key: {type}.")
				.WithLocation(parameterSyntax.GetLocation());

		var originalType = (INamedTypeSymbol) type.OriginalDefinition;
		var @params = new ITypeSymbol[formatter.GenericParams.Length];

		for (int paramIndex = 0; paramIndex < formatter.GenericParams.Length; paramIndex++)
			@params[paramIndex] = formatter.GenericParams[paramIndex].GetType(gsb.Template) ?? throw new ArgumentException(
					$"Can't get type of formatter param (key: {type}) by index {paramIndex}.")
				.WithLocation(parameterSyntax.GetLocation());

		gsb.AppendWith(parameterSyntax.Modifiers.ToFullString(), " ")
			.AppendWith(originalType.Construct(@params).ToDisplayString(), " ")
			.Append(parameterSyntax.Identifier.ToFullString());
	}
}
