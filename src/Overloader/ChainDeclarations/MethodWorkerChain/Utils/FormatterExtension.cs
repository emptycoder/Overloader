using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain.Utils;

internal static class FormatterExtension
{
	public static void AppendFormatter(this GeneratorSourceBuilder gsb,
		ITypeSymbol type,
		string paramName,
		List<(string, string)> replacementModifiers)
	{
		if (!gsb.TryGetFormatter(type, out var formatter))
			throw new ArgumentException();

		if (formatter.Params.Length == 0) throw new ArgumentException();
		int paramIndex = 0;
		AppendFormatterParam();
		for (paramIndex = 1; paramIndex < formatter.Params.Length; paramIndex++)
		{
			gsb.AppendWoTrim(", ");
			AppendFormatterParam();
		}

		void AppendFormatterParam()
		{
			var formatterParam = formatter.Params[paramIndex];
			gsb.AppendWith((formatterParam.GetType(gsb.Template) ??
			                type.GetMemberType(formatterParam.Name!)).ToDisplayString(), " ")
				.Append(paramName)
				.Append(formatterParam.Name);
			replacementModifiers.Add(($"{paramName}.{formatterParam.Name}", $"{paramName}{formatterParam.Name}"));
		}
	}

	public static void AppendFormatterIntegrity(this GeneratorSourceBuilder gsb, ITypeSymbol type, ParameterSyntax parameterSyntax)
	{
		if (!gsb.TryGetFormatter(type, out var formatter))
			throw new ArgumentException($"Can't get formatter by key: {type}.");

		var originalType = (INamedTypeSymbol) type.OriginalDefinition;
		var @params = new ITypeSymbol[formatter.GenericParams.Length];

		for (int paramIndex = 0; paramIndex < formatter.GenericParams.Length; paramIndex++)
			@params[paramIndex] = formatter.GenericParams[paramIndex].GetType(gsb.Template) ?? throw new ArgumentException(
				$"Can't get type of formatter param (key: {type}) by index {paramIndex}.");

		gsb.AppendWith(parameterSyntax.Modifiers.ToFullString(), " ")
			.AppendWith(originalType.Construct(@params).ToDisplayString(), " ")
			.Append(parameterSyntax.Identifier.ToFullString());
	}
}
