using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ContentBuilders;
using Overloader.Entities;
using Overloader.Exceptions;

namespace Overloader.Utils;

public static class SourceBuilderExtensions
{
	public static SourceBuilder AppendUsings(this SourceBuilder sb, SyntaxNode syntax)
	{
		foreach (var @using in syntax.DescendantNodes().Where(node => node is UsingDirectiveSyntax))
			sb.Append(@using.ToFullString(), 1);

		return sb.Append(string.Empty, 1);
	}

	public static SourceBuilder AppendNamespace(this SourceBuilder sb, string? @namespace)
	{
		if (string.IsNullOrWhiteSpace(@namespace)) return sb;
		return sb.AppendAsConstant("namespace")
			.WhiteSpace()
			.Append(@namespace!)
			.AppendAsConstant(";");
	}

	public static SourceBuilder AppendRefReturnValues(this SourceBuilder sb, TypeSyntax typeSyntax)
	{
		if (typeSyntax is not RefTypeSyntax refSyntax) return sb;
		return sb.Append(refSyntax.RefKeyword.ToFullString())
			.Append(refSyntax.ReadOnlyKeyword.ToFullString());
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
				if (Constants.AttributesToRemove.Contains(attr.Name.ToString())) continue;
				if (!isOpened && (isOpened = true))
				{
					var target = listOfAttrs.Target;
					sb.AppendAsConstant("(");
					if (target is not null)
						sb.Append(target.ToString())
							.WhiteSpace();
				}
				else
					sb.AppendAsConstant(",")
						.WhiteSpace();

				sb.Append(attr.ToString());
			}

			if (isOpened) sb.AppendAsConstant(")")
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
							$"Modifier has already been replaced by another {nameof(Modifier)}.")
						.WithLocation(parameter);

				isReplaced = true;
				builder.Append(modifierStr)
					.WhiteSpace();
			}

			if (!isReplaced) 
				builder.Append(modifierText)
					.WhiteSpace();
		}

		foreach ((string? modifierStr, string? insteadOf, var typeSymbol) in parameterData.ModifierChangers)
		{
			if (insteadOf is not null) continue;
			if (typeSymbol is null
			    || SymbolEqualityComparer.Default.Equals(clearType, typeSymbol)
			    || SymbolEqualityComparer.Default.Equals(originalType, typeSymbol))
				builder.Append(modifierStr)
					.AppendAsConstant(separator);
		}

		return builder;
	}
	
	public static SourceBuilder AppendXmlDocumentation(
		this SourceBuilder sourceBuilder,
		XmlDocumentation documentation)
	{
		XmlNodeSyntax? lastXmlText = null;
		foreach (var data in documentation.Trivia)
		{
			switch (data.Kind())
			{
				case SyntaxKind.SingleLineDocumentationCommentTrivia:
					var content = ((DocumentationCommentTriviaSyntax) data.GetStructure()!).Content;
					foreach (var xmlNode in content)
					{
						if (xmlNode is not XmlElementSyntax elementSyntax
						    || elementSyntax.StartTag.Name.LocalName.Text is not "param"
						    || elementSyntax.StartTag.Attributes.FirstOrDefault(attr => attr.Name.LocalName.Text is "name")
							    is not XmlNameAttributeSyntax xmlNameAttributeSyntax)
						{
							if (xmlNode is XmlTextSyntax)
								lastXmlText = xmlNode;
							else
							{
								if (lastXmlText is not null)
								{
									sourceBuilder
										.AppendWoTrim(lastXmlText.ToFullString())
										.WhiteSpace();
									lastXmlText = null;
								}
								sourceBuilder.AppendWoTrim(xmlNode.ToFullString());
							}
							continue;
						}

						string varName = xmlNameAttributeSyntax.Identifier.ToString();
						foreach (string identifier in documentation.ParamsMap[varName])
						{
							sourceBuilder
								.AppendWoTrim(lastXmlText!.ToFullString())
								.WhiteSpace()
								.AppendWoTrim(elementSyntax.ToFullString().Replace($"name=\"{varName}\"", $"name=\"{identifier}\""));
						}
						lastXmlText = null;
					}

					if (lastXmlText is not null)
						sourceBuilder
							.Append(lastXmlText.ToFullString());
					break;
				case SyntaxKind.MultiLineDocumentationCommentTrivia:
					throw new NotSupportedException();
				case SyntaxKind.EndOfLineTrivia:
				case SyntaxKind.WhitespaceTrivia:
					break;
				default:
					sourceBuilder.Append(data.ToFullString());
					break;
			}
		}

		return sourceBuilder;
	}
}
