using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities.Attributes;

public record ChangeModifierDto(
	string Modifier,
	string NewModifier,
	byte TemplateIndexFor,
	ITypeSymbol? TemplateTypeFor)
{
	public static ChangeModifierDto Parse(AttributeSyntax attribute, Compilation compilation)
	{
		var args = attribute.ArgumentList?.Arguments ?? new SeparatedSyntaxList<AttributeArgumentSyntax>();
		if (args.Count < 2)
			throw new ArgumentException("Not enough arguments were specified")
				.WithLocation(attribute);

		string modifier = args[0].Expression.GetInnerText();
		string newModifier = args[1].Expression.GetInnerText();
		
		const int templateForArgIndex = 2;
		byte templateIndexFor = 0;
		ITypeSymbol? templateTypeFor = null;
		
		switch (args.Count)
		{
			case templateForArgIndex:
				break;
			case templateForArgIndex + 1 when args[templateForArgIndex].NameColon is {Name.Identifier.ValueText: "templateTypeFor"}:
				templateTypeFor = args[templateForArgIndex].Expression.GetType(compilation);
				break;
			case templateForArgIndex + 2:
				templateIndexFor = byte.Parse(args[templateForArgIndex].GetText().ToString());
				templateTypeFor = args[templateForArgIndex + 1].Expression.GetType(compilation);
				break;
			default:
				throw new ArgumentException("Wrong count of arguments were specified")
					.WithLocation(attribute);
		}

		return new ChangeModifierDto(modifier, newModifier, templateIndexFor, templateTypeFor);
	}
}
