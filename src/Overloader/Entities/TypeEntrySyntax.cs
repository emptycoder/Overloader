using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Overloader.Entities;

internal struct TypeEntrySyntax
{
	private readonly LazyList<(string ClassName, AttributeArgumentSyntax TypeSyntax)> _overloadTypes = new();

	public readonly TypeDeclarationSyntax Syntax;
	public List<(string ClassName, AttributeArgumentSyntax TypeSyntax)> OverloadTypes => _overloadTypes.Value;
	public string[]? FormattersToUse;
	public bool IsBlackListMode = false;
	public bool IgnoreTransitions = false;
	public TypeSyntax? DefaultType;

	public TypeEntrySyntax() => throw new NotSupportedException();
	public TypeEntrySyntax(TypeDeclarationSyntax syntax) => Syntax = syntax;

	private class LazyList<T>
	{
		private List<T>? _list;
		public List<T> Value => _list ??= new List<T>();
	}
}
