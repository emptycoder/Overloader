using Microsoft.CodeAnalysis;

namespace Overloader.Entities;

internal sealed class StoreDictionary
{
	public sbyte CombineParametersCount;
	public sbyte FormattersIntegrityCount;
	public sbyte FormattersWoIntegrityCount;
	public bool IsNeedToRemoveBody;
	public bool IsSmthChanged;
	public bool SkipMember;
	public string[]? Modifiers;
	public ParameterData[]? OverloadMap;
	public ITypeSymbol ReturnType = default!;
}
