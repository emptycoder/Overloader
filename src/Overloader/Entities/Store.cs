using Microsoft.CodeAnalysis;

namespace Overloader.Entities;

internal sealed class Store
{
	public sbyte CombineParametersCount;
	public sbyte FormattersIntegrityCount;
	public sbyte FormattersWoIntegrityCount;
	public bool IsNeedToRemoveBody;
	public bool IsSmthChanged;
	public MethodData MethodData;
	public ParameterData[]? OverloadMap;
	public bool SkipMember;
}

internal struct MethodData
{
	public string[]? MethodModifiers;
	public ITypeSymbol? ReturnType;
	public string MethodName;
}
