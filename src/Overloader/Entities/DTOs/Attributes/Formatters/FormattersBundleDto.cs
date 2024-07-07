using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities.DTOs.Attributes.Formatters;

public sealed record FormattersBundleDto(string Identifier, string[] FormatterNames)
{
	public readonly string[] FormatterNames = FormatterNames;
	public readonly string Identifier = Identifier;

	public static FormattersBundleDto Parse(AttributeSyntax formatterSyntax, Compilation compilation)
	{
		var args = formatterSyntax.ArgumentList?.Arguments ??
		           throw new ArgumentException("Argument list for formatter can't be null.")
			           .WithLocation(formatterSyntax);
		if (args.Count < 2)
			throw new ArgumentException("Not enough parameters for formatter.")
				.WithLocation(formatterSyntax);

		if (args[0].Expression is not LiteralExpressionSyntax identifier)
			throw new ArgumentException("Identifier should be LiteralExpressionSyntax.")
				.WithLocation(args[0].Expression);

		string[] formatterNames = new string[args.Count - 1];
		for (int index = 1; index < args.Count; index++)
		{
			if (args[index].Expression is not LiteralExpressionSyntax formatterName)
				throw new ArgumentException("Identifier should be LiteralExpressionSyntax.")
					.WithLocation(args[index].Expression);
			formatterNames[index - 1] = formatterName.GetInnerText();
		}

		return new FormattersBundleDto(
			identifier.GetInnerText(),
			formatterNames
		);
	}
}
