using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities.ContentBuilders;

namespace Overloader.Utils;

internal static class SourceBuilderExtensions
{
	public static SourceBuilder AppendUsings(this SourceBuilder sb, SyntaxNode syntax)
	{
		foreach (var @using in syntax.DescendantNodes().Where(node => node is UsingDirectiveSyntax))
			sb.Append(@using.ToFullString(), 1);

		return sb.Append(string.Empty, 1);
	}

	public static SourceBuilder AppendNamespace(this SourceBuilder sb, string @namespace) =>
		sb.AppendWith("namespace", " ")
			.AppendWith(@namespace, ";");

	public static SourceBuilder AppendRefReturnValues(this SourceBuilder sb, TypeSyntax typeSyntax)
	{
		if (typeSyntax is not RefTypeSyntax refSyntax) return sb;
		return sb.AppendWoTrim(refSyntax.RefKeyword.ToFullString())
			.AppendWoTrim(refSyntax.ReadOnlyKeyword.ToFullString());
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
					sb.Append(SyntaxKind.OpenBracketToken);
					if (target is not null)
						sb.AppendWith(target.ToString(), " ");
				}
				else
					sb.AppendWoTrim(", ");

				sb.Append(attr.ToString());
			}

			if (isOpened) sb.Append(SyntaxKind.CloseBracketToken).AppendWoTrim(separator);
		}

		return sb;
	}
}
