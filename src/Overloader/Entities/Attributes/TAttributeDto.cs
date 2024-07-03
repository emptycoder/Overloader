using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities.Attributes;

public record TAttributeDto(
	byte TemplateIndex,
	ITypeSymbol? NewType,
	ITypeSymbol? ForType)
{
	public static TAttributeDto Parse(AttributeSyntax attribute, Compilation compilation)
	{
		var args = attribute.ArgumentList?.Arguments ?? new SeparatedSyntaxList<AttributeArgumentSyntax>();
		byte templateIndex;
		ITypeSymbol? newType = null;
		ITypeSymbol? forType = null;
		
		switch (args.Count)
		{
			case 0:
				templateIndex = 0;
				break;
			case 1 when args[0].NameColon is {Name.Identifier.ValueText: "newType"}:
				newType = args[0].GetType(compilation);
				goto case 0;
			case 1:
				templateIndex = byte.Parse(args[0].GetText().ToString());
				break;
			case 2 when args[0].NameColon is {Name.Identifier.ValueText: "newType"}
				&& args[1].NameColon is {Name.Identifier.ValueText: "forType"}:
				newType = args[0].GetType(compilation);
				forType = args[1].GetType(compilation);
				goto case 0;
			case 2:
				newType = args[1].GetType(compilation);
				goto case 1;
			case 3:
				forType = args[2].GetType(compilation);
				goto case 2;
			default:
				throw new ArgumentException($"Unexpected count of arguments in {TAttribute.TagName}.")
					.WithLocation(attribute);
		}

		return new TAttributeDto(templateIndex, newType, forType);
	}
}