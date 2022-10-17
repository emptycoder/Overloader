using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities.Builders;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain.ChainUtils;

internal static class MethodHeaderExtension
{
	public static SourceBuilder AppendMethodDeclarationSpecifics(this SourceBuilder sb,
		MethodDeclarationSyntax syntax,
		string[] modifiers,
		ITypeSymbol? returnType) =>
		sb.AppendAttributes(syntax.AttributeLists, "\n")
			.AppendWith(string.Join(" ", modifiers), " ")
			.AppendRefReturnValues(syntax.ReturnType)
			.AppendWith(returnType?.ToDisplayString() ?? syntax.ReturnType.ToFullString(), " ")
			.Append(syntax.Identifier.ToFullString());

	public static SourceBuilder AppendParameter(this SourceBuilder sb,
		ParameterSyntax parameter,
		ITypeSymbol newType,
		Compilation compilation) =>
		sb.AppendAttributes(parameter.AttributeLists, " ")
			.AppendWith(parameter.Type!.GetType(compilation)
				.ConstructWithClearType(newType, compilation).ToDisplayString(), " ")
			.Append(parameter.Identifier.ToString());
}
