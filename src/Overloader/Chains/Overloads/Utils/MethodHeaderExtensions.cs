using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ContentBuilders;
using Overloader.Entities;
using Overloader.Entities.DTOs;
using Overloader.Utils;

namespace Overloader.Chains.Overloads.Utils;

public static class MethodHeaderExtensions
{
	public static SourceBuilder AppendMethodDeclarationSpecifics(
		this SourceBuilder sb,
		MethodDeclarationSyntax syntax,
		MethodDataDto dataDto) =>
		sb.AppendAttributes(syntax.AttributeLists, "\n")
			.TrimAppend(dataDto.MethodModifiers is null ? string.Empty : string.Join(" ", dataDto.MethodModifiers))
			.WhiteSpace()
			.AppendRefReturnValues(syntax.ReturnType)
			.TrimAppend(dataDto.ReturnType?.ToDisplayString() ?? syntax.ReturnType.ToFullString())
			.WhiteSpace()
			.TrimAppend(dataDto.MethodName ?? throw new NotSupportedException("Method name should be declared"))
			.TrimAppend(syntax.TypeParameterList?.ToString() ?? string.Empty);

	public static SourceBuilder AppendParameter(
		this SourceBuilder sb,
		ParameterSyntax parameter,
		ParameterDataDto mappedParam,
		Compilation compilation)
	{
		var newType = parameter.Type!.GetType(compilation)
			.ConstructWithClearType(mappedParam.Type, compilation);

		return sb.AppendAttributes(parameter.AttributeLists, " ")
			.AppendAndBuildModifiers(mappedParam, parameter, " ")
			.TrimAppend(newType.ToDisplayString())
			.WhiteSpace()
			.TrimAppend(parameter.Identifier.ToString());
	}
}
