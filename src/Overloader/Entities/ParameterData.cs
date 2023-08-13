using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ContentBuilders;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities;

public sealed record ParameterData(
	ParameterAction ParameterAction,
	ITypeSymbol Type,
	List<(string Modifier, string? InsteadOf, ITypeSymbol? FormatterType)> ModifierChangers,
	byte CombineIndex)
{
	public bool IsCombineNotExists => CombineIndex == byte.MaxValue;
}
