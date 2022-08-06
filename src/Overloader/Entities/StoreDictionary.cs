using System.Buffers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Enums;

namespace Overloader.Entities;

internal sealed class StoreDictionary : IDisposable
{
	public bool IsAnyFormatter;
	public bool IsSmthChanged;
	public (ParameterAction ParameterAction, ITypeSymbol Type)[]? OverloadMap;
	public TypeSyntax ReturnType = default!;

	public static StoreDictionary Shared => new();

	public void Dispose() =>
		ArrayPool<(ParameterAction ParameterAction, ITypeSymbol Type)>.Shared.Return(OverloadMap);
}
