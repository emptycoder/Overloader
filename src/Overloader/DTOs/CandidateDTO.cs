using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Overloader.DTOs;

internal struct CandidateDto
{
	private LazyList<OverloadDto> _overloadTypes = new();

	public readonly TypeDeclarationSyntax Syntax;
	public List<OverloadDto> OverloadTypes => _overloadTypes.Value;
	public string[]? FormattersToUse;
	public bool IsBlackListMode = false;
	public bool IgnoreTransitions = false;
	public TypeSyntax? DefaultType;

	public CandidateDto() => throw new NotSupportedException();
	public CandidateDto(TypeDeclarationSyntax syntax) => Syntax = syntax;

	private struct LazyList<T>
	{
		private List<T>? _list;
		public List<T> Value => _list ??= new List<T>();
	}
}
