using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Overloader.Entities;

internal readonly struct TypeEntrySyntax
{
	public readonly TypeDeclarationSyntax Syntax;
	public readonly List<(string ClassName, AttributeArgumentSyntax TypeSyntax)> OverloadTypes = new();
	public readonly List<AttributeSyntax> FormatterSyntaxes = new();

	public TypeEntrySyntax(TypeDeclarationSyntax syntax) => Syntax = syntax;
}
