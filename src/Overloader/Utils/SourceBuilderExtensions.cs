using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ContentBuilders;
using Overloader.Entities;
using Overloader.Exceptions;

namespace Overloader.Utils;

public static class SourceBuilderExtensions
{
	private static readonly HashSet<string> AttributesToRemove =
	[
		Formatter.TagName,
		FormattersBundle.TagName,
		/* Method */
		SkipMode.TagName,
		ChangeModifier.TagName,
		ChangeName.TagName,
		ForceChanged.TagName,
		/* Parameter */
		CombineWith.TagName,
		Integrity.TagName,
		Modifier.TagName,
		Ref.TagName,
		TAttribute.TagName,
		/* Type */
		InvertedMode.TagName,
		IgnoreTransitions.TagName,
		RemoveBody.TagName,
		TOverload.TagName,
		TSpecify.TagName
	];
	
	public static SourceBuilder AppendUsings(this SourceBuilder sb, SyntaxNode syntax)
	{
		foreach (var @using in syntax.DescendantNodes().Where(node => node is UsingDirectiveSyntax))
			sb.TrimAppend(@using.ToFullString(), 1);

		return sb.TrimAppend(string.Empty, 1);
	}

	public static SourceBuilder AppendNamespace(this SourceBuilder sb, string? @namespace)
	{
		if (string.IsNullOrWhiteSpace(@namespace)) return sb;
		return sb.AppendAsConstant("namespace")
			.WhiteSpace()
			.TrimAppend(@namespace!)
			.AppendAsConstant(";");
	}

	public static SourceBuilder AppendRefReturnValues(this SourceBuilder sb, TypeSyntax typeSyntax)
	{
		if (typeSyntax is not RefTypeSyntax refSyntax) return sb;
		return sb.TrimAppend(refSyntax.RefKeyword.ToFullString())
			.TrimAppend(refSyntax.ReadOnlyKeyword.ToFullString())
			.WhiteSpace();
	}

	public static SourceBuilder AppendAttributes(
		this SourceBuilder sb,
		in SyntaxList<AttributeListSyntax> attributeListSyntaxes,
		string separator)
	{
		foreach (var listOfAttrs in attributeListSyntaxes)
		{
			bool isOpened = false;
			foreach (var attr in listOfAttrs.Attributes)
			{
				if (AttributesToRemove.Contains(attr.Name.ToString())) continue;
				if (!isOpened && (isOpened = true))
				{
					var target = listOfAttrs.Target;
					sb.AppendAsConstant("[");
					if (target is not null)
						sb.TrimAppend(target.ToString())
							.WhiteSpace();
				}
				else
					sb.AppendAsConstant(",")
						.WhiteSpace();

				sb.TrimAppend(attr.ToString());
			}

			if (isOpened)
				sb.AppendAsConstant("]")
					.AppendAsConstant(separator);
		}

		return sb;
	}

	public static SourceBuilder AppendAndBuildModifiers(
		this SourceBuilder builder,
		ParameterData parameterData,
		ParameterSyntax parameter,
		ITypeSymbol newParamType,
		string separator)
	{
		var clearType = newParamType.GetClearType();
		var originalType = clearType.OriginalDefinition;
		foreach (var modifier in parameter.Modifiers)
		{
			bool isReplaced = false;
			string modifierText = modifier.Text;
			foreach ((string? modifierStr, string? insteadOf, var typeSymbol) in parameterData.ModifierChangers)
			{
				if (insteadOf is null) continue;
				if (modifierText != insteadOf) continue;
				if (typeSymbol is not null
				    && !SymbolEqualityComparer.Default.Equals(clearType, typeSymbol)
				    && !SymbolEqualityComparer.Default.Equals(originalType, typeSymbol)) continue;
				if (isReplaced)
					throw new ArgumentException(
							$"Modifier has already been replaced by another {Modifier.TagName}.")
						.WithLocation(parameter);

				isReplaced = true;
				builder.TrimAppend(modifierStr)
					.WhiteSpace();
			}

			if (!isReplaced)
				builder.TrimAppend(modifierText)
					.WhiteSpace();
		}

		foreach ((string? modifierStr, string? insteadOf, var typeSymbol) in parameterData.ModifierChangers)
		{
			if (insteadOf is not null) continue;
			if (typeSymbol is null
			    || SymbolEqualityComparer.Default.Equals(clearType, typeSymbol)
			    || SymbolEqualityComparer.Default.Equals(originalType, typeSymbol))
				builder.TrimAppend(modifierStr)
					.AppendAsConstant(separator);
		}

		return builder;
	}
}
