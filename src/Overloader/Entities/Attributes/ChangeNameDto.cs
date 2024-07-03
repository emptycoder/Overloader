using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities.Attributes;

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
		
		const int templateForArgIndex = 1;
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

		return new ChangeNameDto(newName, templateIndexFor, templateTypeFor);
	}
}
