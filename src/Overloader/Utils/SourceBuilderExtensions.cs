using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;

namespace Overloader.Utils;

internal static class SourceBuilderExtensions
{
	public static SourceBuilder AppendUsings(this SourceBuilder sb, SyntaxNode syntax)
	{
		foreach (var @using in syntax.DescendantNodes().Where(node => node is UsingDirectiveSyntax))
			sb.Append(@using.ToFullString(), 1);

		return sb.Append(string.Empty, 1);
	}

	public static SourceBuilder AppendMethodDeclarationSpecifics(this SourceBuilder sb,
		MethodDeclarationSyntax syntax,
		string[] modifiers,
		ITypeSymbol? returnType) =>
		sb.Append(syntax.AttributeLists.ToFullString(), 1)
			.AppendWith(string.Join(" ", modifiers), " ")
			.AppendWoTrim(syntax.ReturnType.GetPreTypeValues())
			.AppendWith(returnType?.ToDisplayString() ?? syntax.ReturnType.ToFullString(), " ")
			.Append(syntax.Identifier.ToFullString());

	public static SourceBuilder AppendParameter(this SourceBuilder sb,
		ParameterSyntax parameter,
		ITypeSymbol newType,
		Compilation compilation) =>
		sb.AppendWith(parameter.AttributeLists.ToFullString(), " ")
			.AppendWith(parameter.Type!.GetType(compilation)
				.ConstructWithClearType(newType, compilation).ToDisplayString(), " ")
			.Append(parameter.Identifier.ToString());
}
