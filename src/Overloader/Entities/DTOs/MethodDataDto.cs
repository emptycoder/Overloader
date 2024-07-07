using Microsoft.CodeAnalysis;

namespace Overloader.Entities.DTOs;

public record struct MethodDataDto
{
	public ParameterDataDto[] Parameters;
	public string[]? MethodModifiers;
	public ITypeSymbol? ReturnType;
	public string? MethodName;
}
