using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Overloader.Entities;

#nullable disable
public sealed class Store
{
	public byte CombineParametersCount;
	public bool IsSmthChanged;
	public MethodData MethodData;
	public MethodDeclarationSyntax MethodSyntax;
	public ParameterData[] OverloadMap;
	public bool ShouldRemoveBody;
	public bool SkipMember;
	public XmlDocumentation XmlDocumentation;
}
