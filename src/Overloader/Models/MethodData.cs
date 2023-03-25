using Microsoft.CodeAnalysis;

namespace Overloader.Models;

public struct MethodData
{
	public string[]? MethodModifiers;
	public ITypeSymbol? ReturnType;
	public string MethodName;
}
