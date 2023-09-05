using Microsoft.CodeAnalysis;
using Overloader.Enums;

namespace Overloader.Entities;

public sealed record ParameterData(
	ParameterReplacement ReplacementType,
	ITypeSymbol Type,
	List<(string Modifier, string? InsteadOf, ITypeSymbol? FormatterType)> ModifierChangers,
	byte CombineIndex)
{
	public bool IsCombineNotExists => CombineIndex == byte.MaxValue;
}
