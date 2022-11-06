using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Overloader.Entities;

internal struct TypeEntrySyntax
{
	private readonly LazyList<(string ClassName, AttributeArgumentSyntax TypeSyntax)> _overloadTypes = new();
	private readonly LazyList<string> _formattersToUse = new();
	
	public readonly TypeDeclarationSyntax Syntax;
	public List<(string ClassName, AttributeArgumentSyntax TypeSyntax)> OverloadTypes => _overloadTypes.Value;
	public List<string> FormattersToUse => _formattersToUse.Value;
	public bool IsBlackListMode = false;
	public TypeSyntax? DefaultType;

	public TypeEntrySyntax() => throw new NotSupportedException();
	public TypeEntrySyntax(TypeDeclarationSyntax syntax) => Syntax = syntax;

	private class LazyList<T>
	{
		private List<T>? _list;
		public List<T> Value => _list ??= new List<T>();
	}
}
