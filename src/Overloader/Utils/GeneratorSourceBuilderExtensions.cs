using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;

namespace Overloader.Utils;

internal static class GeneratorSourceBuilderExtensions
{
	public static GeneratorSourceBuilder AppendUsings(this GeneratorSourceBuilder gsb, SyntaxNode syntax)
	{
		foreach (var @using in syntax.DescendantNodes().Where(node => node is UsingDirectiveSyntax))
			gsb.Append(@using.ToFullString(), 1);

		return gsb.Append(string.Empty, 1);
	}
}
