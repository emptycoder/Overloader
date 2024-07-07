using Microsoft.CodeAnalysis;

namespace Overloader.Entities.DTOs.Attributes.Formatters.Transitions;

public record struct ParamDto
{
	public string Name { get; init; }
	public string Modifier { get; init; }
	public ITypeSymbol Type { get; init; }
	public bool IsUnboundType { get; init; }
}
