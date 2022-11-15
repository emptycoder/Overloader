using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Entities.ContentBuilders;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain.ChainUtils;

internal static class MethodHeaderExtension
{
	public static SourceBuilder AppendMethodDeclarationSpecifics(
		this SourceBuilder sb,
		MethodDeclarationSyntax syntax,
		string[] modifiers,
		ITypeSymbol? returnType) =>
		sb.AppendAttributes(syntax.AttributeLists, "\n")
			.AppendWith(string.Join(" ", modifiers), " ")
			.AppendRefReturnValues(syntax.ReturnType)
			.AppendWith(returnType?.ToDisplayString() ?? syntax.ReturnType.ToFullString(), " ")
			.Append(syntax.Identifier.ToFullString());

	public static SourceBuilder AppendParameter(
		this SourceBuilder sb,
		ParameterSyntax parameter,
		ParameterData mappedParam,
		Compilation compilation)
	{
		var newType = parameter.Type!.GetType(compilation)
			.ConstructWithClearType(mappedParam.Type, compilation);

		return sb.AppendAttributes(parameter.AttributeLists, " ")
			.AppendWoTrim(mappedParam.BuildModifiersWithWhitespace(parameter, newType))
			.AppendWith(newType.ToDisplayString(), " ")
			.Append(parameter.Identifier.ToString());
	}
}
