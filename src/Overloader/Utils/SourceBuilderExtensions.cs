using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;

namespace Overloader.Utils;

internal static class SourceBuilderExtensions
{
	public static SourceBuilder AppendUsings(this SourceBuilder gsb, SyntaxNode syntax)
	{
		foreach (var @using in syntax.DescendantNodes().Where(node => node is UsingDirectiveSyntax))
			gsb.Append(@using.ToFullString(), 1);

		return gsb.Append(string.Empty, 1);
	}
}
