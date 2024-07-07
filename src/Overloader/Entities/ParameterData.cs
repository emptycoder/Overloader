using Microsoft.CodeAnalysis;
using Overloader.Entities.Attributes;
using Overloader.Enums;

namespace Overloader.Entities;

public sealed record ParameterData(
	byte TemplateIndex,
	RequiredReplacement ReplacementType,
	ITypeSymbol Type,
	List<ModifierDto> ModifierChangers,
	byte CombineIndex)
{
	public bool IsCombineNotExists => CombineIndex == byte.MaxValue;
}
