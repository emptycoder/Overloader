using Microsoft.CodeAnalysis;

namespace Overloader.Entities;

internal sealed class StoreDictionary
{
	public ushort FormattersWoIntegrityCount;
	public ushort CombineParametersCount;
	public bool IsSmthChanged;
	public bool MemberSkip;
	public string[]? Modifiers;
	public ParameterData[]? OverloadMap;
	public ITypeSymbol ReturnType = default!;
}
