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
		string newName;
		byte templateIndexFor = 0;
		ITypeSymbol? templateTypeFor = null;
		
		var args = attribute.ArgumentList?.Arguments ?? [];
		switch (args.Count)
		{
			case 1:
				newName = args[0].Expression.GetVariableName();
				break;
			case 2 when args[1].NameColon is {Name.Identifier.ValueText: "templateTypeFor"}:
				templateTypeFor = args[1].Expression.GetType(compilation);
				goto case 1;
			case 4:
				templateIndexFor = byte.Parse(args[2].GetText().ToString());
				templateTypeFor = args[3].Expression.GetType(compilation);
				goto case 1;
			default:
				throw new ArgumentException("Wrong count of arguments were specified")
					.WithLocation(attribute);
		}
		
		return new ChangeNameDto(newName, templateIndexFor, templateTypeFor);
	}
}
