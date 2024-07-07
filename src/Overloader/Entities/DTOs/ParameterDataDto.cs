using Microsoft.CodeAnalysis;
using Overloader.Entities.DTOs.Attributes;
using Overloader.Enums;

namespace Overloader.Entities.DTOs;

public sealed record ParameterDataDto(
	byte TemplateIndex,
	RequiredReplacement ReplacementType,
	ITypeSymbol Type,
	List<ModifierDto> ModifierChangers,
	byte CombineIndex)
{
	public bool IsCombineNotExists => CombineIndex == byte.MaxValue;
}
