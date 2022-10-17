using Microsoft.CodeAnalysis;
using Overloader.Enums;

namespace Overloader.Entities;

internal sealed record ParameterData(
	ParameterAction ParameterAction,
	ITypeSymbol Type,
	sbyte CombineIndex)
{
	public bool IsCombineNotExists => CombineIndex == sbyte.MaxValue;
}
