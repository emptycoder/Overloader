using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Overloader.Entities;

internal struct TypeEntrySyntax
{
	public readonly TypeDeclarationSyntax Syntax;
	private LazyList<(string ClassName, AttributeArgumentSyntax TypeSyntax)> _overloadTypes = new();
	public List<(string ClassName, AttributeArgumentSyntax TypeSyntax)> OverloadTypes => _overloadTypes.Value;
	private LazyList<AttributeSyntax> _formatterSyntaxes = new();
	public List<AttributeSyntax> FormatterSyntaxes => _formatterSyntaxes.Value;
	public bool IsBlackListMode = false;
	public TypeSyntax? DefaultType;

	public TypeEntrySyntax() => throw new NotSupportedException();
	public TypeEntrySyntax(TypeDeclarationSyntax syntax) => Syntax = syntax;

	private struct LazyList<T>
	{
		private List<T>? _list;
		public List<T> Value => _list ??= new List<T>();
	}
}
