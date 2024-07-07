using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities.DTOs.Attributes.Formatters;
using Overloader.Exceptions;

namespace Overloader.Utils;

public static class FormatterExtensions
{
	public static Dictionary<string, FormatterDto> GetFormatters(this IEnumerable<AttributeSyntax> attributeSyntaxes, Compilation compilation)
	{
		var dict = new Dictionary<string, FormatterDto>();
		foreach (var formatterSyntax in attributeSyntaxes)
		{
			if (formatterSyntax.Name.GetName() is not Formatter.TagName) continue;

			var formatter = FormatterDto.Parse(formatterSyntax, compilation);
			if (dict.ContainsKey(formatter.Identifier))
				throw new ArgumentException($"{Formatter.TagName} with identifier '{formatter.Identifier}' has been already exist.")
					.WithLocation(formatterSyntax);

			dict.Add(formatter.Identifier, formatter);
		}

		return dict;
	}

	public static Dictionary<string, FormattersBundleDto> GetBundles(this IEnumerable<AttributeSyntax> attributeSyntaxes, Compilation compilation)
	{
		var dict = new Dictionary<string, FormattersBundleDto>();
		foreach (var formatterSyntax in attributeSyntaxes)
		{
			if (formatterSyntax.Name.GetName() is not FormattersBundle.TagName) continue;

			var formattersBundle = FormattersBundleDto.Parse(formatterSyntax, compilation);
			if (dict.ContainsKey(formattersBundle.Identifier))
				throw new ArgumentException($"{FormattersBundle.TagName} with identifier '{formattersBundle.Identifier}' has been already exist.")
					.WithLocation(formatterSyntax);

			dict.Add(formattersBundle.Identifier, formattersBundle);
		}

		return dict;
	}

	public static Dictionary<ITypeSymbol, FormatterDto>? GetFormattersSample(
		this Dictionary<string, FormatterDto> globalFormatters,
		Dictionary<string, FormattersBundleDto> formattersBundles,
		string[]? formattersToUse,
		Location location)
	{
		if (formattersToUse is null) return null;

		var formatters = new Dictionary<ITypeSymbol, FormatterDto>(formattersToUse.Length, SymbolEqualityComparer.Default);
		foreach (string identifier in formattersToUse)
		{
			if (formattersBundles.TryGetValue(identifier, out var bundle))
				foreach (string formatterName in bundle.FormatterNames)
					AddFormatterToSample(formatterName);
			else
				AddFormatterToSample(identifier);
		}

		return formatters;

		void AddFormatterToSample(string formatterIdentifier)
		{
			if (!globalFormatters.TryGetValue(formatterIdentifier, out var formatter))
				throw new ArgumentException($"Can't find formatter with identifier '{formatterIdentifier}'.")
					.WithLocation(location);

			foreach (var formatterType in formatter.Types)
			{
				if (formatters.TryGetValue(formatterType, out var sameTypeFormatter))
					throw new ArgumentException($"Type has been already overridden by '{sameTypeFormatter.Identifier}' formatter.")
						.WithLocation(location);
				formatters.Add(formatterType, formatter);
			}
		}
	}
}
