using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ContentBuilders;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Models;

public sealed record ParameterData(
	ParameterAction ParameterAction,
	ITypeSymbol Type,
	List<(string Modifier, string? InsteadOf, ITypeSymbol? FormatterType)> ModifierChangers,
	sbyte CombineIndex)
{
	public bool IsCombineNotExists => CombineIndex == sbyte.MaxValue;

	public string BuildModifiersWithWhitespace(ParameterSyntax parameter, ITypeSymbol newParamType)
	{
		using var sb = SourceBuilder.GetInstance();
		var clearType = newParamType.GetClearType();
		var originalType = clearType.OriginalDefinition;
		foreach (var modifier in parameter.Modifiers)
		{
			bool isReplaced = false;
			string modifierText = modifier.Text;
			foreach ((string? modifierStr, string? insteadOf, var typeSymbol) in ModifierChangers)
			{
				if (insteadOf is null) continue;
				if (modifierText != insteadOf) continue;
				if (typeSymbol is not null
				    && !SymbolEqualityComparer.Default.Equals(clearType, typeSymbol)
				    && !SymbolEqualityComparer.Default.Equals(originalType, typeSymbol)) continue;
				if (isReplaced)
					throw new ArgumentException(
							$"Modifier has already been replaced by another {nameof(ParamModifier)}.")
						.WithLocation(parameter);

				isReplaced = true;
				sb.AppendWith(modifierStr, " ");
			}

			if (!isReplaced) sb.AppendWith(modifierText, " ");
		}

		foreach ((string? modifierStr, string? insteadOf, var typeSymbol) in ModifierChangers)
		{
			if (insteadOf is not null) continue;
			if (typeSymbol is null
			    || SymbolEqualityComparer.Default.Equals(clearType, typeSymbol)
			    || SymbolEqualityComparer.Default.Equals(originalType, typeSymbol))
				sb.AppendWith(modifierStr, " ");
		}

		return sb.ToString();
	}
}
