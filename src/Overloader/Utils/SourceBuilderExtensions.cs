using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities.Builders;

namespace Overloader.Utils;

internal static class SourceBuilderExtensions
{
	public static SourceBuilder AppendUsings(this SourceBuilder sb, SyntaxNode syntax)
	{
		foreach (var @using in syntax.DescendantNodes().Where(node => node is UsingDirectiveSyntax))
			sb.Append(@using.ToFullString(), 1);

		return sb.Append(string.Empty, 1);
	}

	public static SourceBuilder AppendNamespace(this SourceBuilder sb, string @namespace) =>
		sb.AppendWith("namespace", " ")
			.AppendWith(@namespace, ";");

	public static SourceBuilder AppendRefReturnValues(this SourceBuilder sb, TypeSyntax typeSyntax)
	{
		if (typeSyntax is not RefTypeSyntax refSyntax) return sb;
		return sb.AppendWoTrim(refSyntax.RefKeyword.ToFullString())
			.AppendWoTrim(refSyntax.ReadOnlyKeyword.ToFullString());
	}
}
