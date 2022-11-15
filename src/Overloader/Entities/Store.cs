using Microsoft.CodeAnalysis;

namespace Overloader.Entities;

internal sealed class Store
{
	public sbyte CombineParametersCount;
	public sbyte FormattersIntegrityCount;
	public sbyte FormattersWoIntegrityCount;
	public bool IsNeedToRemoveBody;
	public bool IsSmthChanged;
	public string[]? Modifiers;
	public ParameterData[]? OverloadMap;
	public ITypeSymbol ReturnType = default!;
	public bool SkipMember;
}
