using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ContentBuilders;
using Overloader.Models;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain.ChainUtils;

public static class MethodHeaderExtension
{
	public static SourceBuilder AppendMethodDeclarationSpecifics(
		this SourceBuilder sb,
		MethodDeclarationSyntax syntax,
		MethodData data) =>
		sb.AppendAttributes(syntax.AttributeLists, "\n")
			.AppendWith(data.MethodModifiers is null ? string.Empty : string.Join(" ", data.MethodModifiers), " ")
			.AppendRefReturnValues(syntax.ReturnType)
			.AppendWith(data.ReturnType?.ToDisplayString() ?? syntax.ReturnType.ToFullString(), " ")
			.Append(data.MethodName)
			.Append(syntax.TypeParameterList?.ToString() ?? string.Empty);

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
