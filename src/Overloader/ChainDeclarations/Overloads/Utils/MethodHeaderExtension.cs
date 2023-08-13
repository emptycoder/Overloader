using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ContentBuilders;
using Overloader.Entities;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.Overloads.Utils;

public static class MethodHeaderExtension
{
	public static SourceBuilder AppendMethodDeclarationSpecifics(
		this SourceBuilder sb,
		MethodDeclarationSyntax syntax,
		MethodData data) =>
		sb.AppendAttributes(syntax.AttributeLists, "\n")
			.Append(data.MethodModifiers is null ? string.Empty : string.Join(" ", data.MethodModifiers))
			.WhiteSpace()
			.AppendRefReturnValues(syntax.ReturnType)
			.Append(data.ReturnType?.ToDisplayString() ?? syntax.ReturnType.ToFullString())
			.WhiteSpace()
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
			.AppendAndBuildModifiers(mappedParam, parameter, newType, " ")
			.Append(newType.ToDisplayString())
			.WhiteSpace()
			.Append(parameter.Identifier.ToString());
	}
}
