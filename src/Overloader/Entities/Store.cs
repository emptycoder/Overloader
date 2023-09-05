using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Overloader.Entities;

#nullable disable
public sealed class Store
{
	public byte CombineParametersCount;
	public bool ShouldRemoveBody;
	public bool IsSmthChanged;
	public MethodData MethodData;
	public ParameterData[] OverloadMap;
	public MethodDeclarationSyntax MethodSyntax;
	public bool SkipMember;
}

