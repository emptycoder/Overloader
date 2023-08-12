using Microsoft.CodeAnalysis;

namespace Overloader.Entities;

public struct MethodData
{
	public string[]? MethodModifiers;
	public ITypeSymbol? ReturnType;
	public string MethodName;
}
