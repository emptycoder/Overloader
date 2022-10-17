using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Overloader.Entities;

// ReSharper disable once InconsistentNaming
public struct TAttributeDTO
{
	public AttributeSyntax Attribute;
	public bool ForceOverloadIntegrity;
	public string? CombineWith;
}
