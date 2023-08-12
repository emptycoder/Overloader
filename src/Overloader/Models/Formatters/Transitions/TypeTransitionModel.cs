using Microsoft.CodeAnalysis;

namespace Overloader.Models.Formatters.Transitions;

public record struct TypeTransitionModel
{
	public string Modifier { get; init; }
	public ITypeSymbol Type { get; init; }
	public bool IsUnboundType { get; init; }
}
