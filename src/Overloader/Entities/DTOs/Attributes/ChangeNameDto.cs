using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities.DTOs.Attributes;

public record ChangeNameDto(
	string NewName,
	byte TemplateIndexFor,
	ITypeSymbol? TemplateTypeFor)
{
	public static ChangeNameDto Parse(AttributeSyntax attribute, Compilation compilation)
	{
		var args = attribute.ArgumentList?.Arguments ?? new SeparatedSyntaxList<AttributeArgumentSyntax>();
		if (args.Count < 1)
			throw new ArgumentException("Not enough arguments were specified")
				.WithLocation(attribute);

		string newName = args[0].Expression.GetVariableName();
		(byte templateIndexFor, var templateTypeFor) = attribute.ParseTemplateFor(compilation, 1);

		return new ChangeNameDto(newName, templateIndexFor, templateTypeFor);
	}
}
