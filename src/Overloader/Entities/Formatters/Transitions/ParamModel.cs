using Microsoft.CodeAnalysis;

namespace Overloader.Entities.Formatters.Transitions;

public record struct ParamModel
{
	public string Name { get; init; }
	public string Modifier { get; init; }
	public ITypeSymbol Type { get; init; }
	public bool IsUnboundType { get; init; }
}
