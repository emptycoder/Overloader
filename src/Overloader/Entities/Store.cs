using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Overloader.Entities;

#nullable disable
public sealed class Store
{
	public byte CombineParametersCount;
	public bool IsSmthChanged;
	public MethodDeclarationSyntax MethodSyntax;
	public MethodData MethodData;
	public bool ShouldRemoveBody;
	public bool SkipMember;
	public XmlDocumentation XmlDocumentation;
}
