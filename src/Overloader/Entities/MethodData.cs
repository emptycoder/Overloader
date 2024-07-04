using Microsoft.CodeAnalysis;

namespace Overloader.Entities;

public record struct MethodData
{
	public ParameterData[] Parameters;
	public string[]? MethodModifiers;
	public ITypeSymbol? ReturnType;
	public string? MethodName;
}
