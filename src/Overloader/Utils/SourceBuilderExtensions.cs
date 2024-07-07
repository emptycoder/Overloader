using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ContentBuilders;
using Overloader.Entities;
using Overloader.Entities.DTOs;
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
		ParameterDataDto parameterDataDto,
		ParameterSyntax parameter,
		string separator)
	{
		foreach (var modifier in parameter.Modifiers)
		{
			bool isReplaced = false;
			string modifierText = modifier.Text;
			foreach (var modifierDto in parameterDataDto.ModifierChangers)
			{
				if (modifierDto.InsteadOf is null) continue;
				if (modifierText != modifierDto.InsteadOf) continue;
				if (isReplaced)
					throw new ArgumentException(
							$"Modifier has already been replaced by another {Modifier.TagName}.")
						.WithLocation(parameter);

				isReplaced = true;
				builder.TrimAppend(modifierDto.Modifier)
					.WhiteSpace();
			}

			if (!isReplaced)
				builder.TrimAppend(modifierText)
					.WhiteSpace();
		}

		foreach (var modifierDto in parameterDataDto.ModifierChangers)
		{
			if (modifierDto.InsteadOf is not null) continue;
			
			builder.TrimAppend(modifierDto.Modifier)
				.AppendAsConstant(separator);
		}

		return builder;
	}
}
