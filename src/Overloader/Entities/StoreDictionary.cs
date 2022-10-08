using System.Buffers;
using Microsoft.CodeAnalysis;

namespace Overloader.Entities;

internal sealed class StoreDictionary : IDisposable
{
	public ushort FormattersWoIntegrityCount;
	public ushort CombineParametersCount;
	public bool IsSmthChanged;
	public bool MemberSkip;
	public string[]? Modifiers;
	public ParameterData[]? OverloadMap;
	public ITypeSymbol ReturnType = default!;

	public void Dispose()
	{
		if (OverloadMap is not null)
			ArrayPool<ParameterData>.Shared.Return(OverloadMap);
	}
}
