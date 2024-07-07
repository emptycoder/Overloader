using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities.DTOs;

namespace Overloader.Entities;

#nullable disable
public sealed class Store
{
	public byte CombineParametersCount;
	public bool IsSmthChanged;
	public MethodDeclarationSyntax MethodSyntax;
	public MethodDataDto MethodDataDto;
	public bool ShouldRemoveBody;
	public bool SkipMember;
	public XmlDocumentation XmlDocumentation;
}
