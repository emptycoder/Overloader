using System.Buffers;
using Microsoft.CodeAnalysis;
using Overloader.Enums;

namespace Overloader.Entities;

internal sealed class StoreDictionary : IDisposable
{
	public bool IsAnyFormatter;
	public bool IsSmthChanged;
	public bool IsPartial;
	public bool MemberSkip;
	public string[]? Modifiers;
	public (ParameterAction ParameterAction, ITypeSymbol Type)[]? OverloadMap;
	public ITypeSymbol ReturnType = default!;

	public void Dispose()
	{
		if (OverloadMap is not null)
			ArrayPool<(ParameterAction ParameterAction, ITypeSymbol Type)>.Shared.Return(OverloadMap);
	}
}
