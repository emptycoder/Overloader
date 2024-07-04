using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities.Attributes;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities.DTOs;

public record struct CandidateDto(
	TypeDeclarationSyntax Syntax,
	// ReSharper disable once InconsistentNaming
	TSpecifyDto TSpecifyDto,
	List<TOverloadDto> OverloadTypes,
	bool IsInvertedMode,
	bool IsTransitionsIgnored)
{
	public static bool TryParse(GeneratorSyntaxContext context, out CandidateDto? candidateDto)
	{
		var declarationSyntax = (TypeDeclarationSyntax) context.Node;
		TSpecifyDto? tSpecifyDto = null;
		bool isTransitionsIgnored = false;
		bool isInvertedMode = false;
		var overloadTypes = new LazyList<TOverloadDto>();

		foreach (var attributeList in declarationSyntax.AttributeLists)
		foreach (var attribute in attributeList.Attributes)
		{
			switch (attribute.Name.GetName())
			{
				case TOverload.TagName when attribute.ArgumentList is not null:
					string className = declarationSyntax.Identifier.ValueText;
					overloadTypes.Value.Add(TOverloadDto.Parse(className, attribute));
					break;
				case IgnoreTransitions.TagName:
					isTransitionsIgnored = true;
					break;
				case InvertedMode.TagName:
					isInvertedMode = true;
					break;
				case TSpecify.TagName:
					tSpecifyDto = TSpecifyDto.Parse(attribute);
					continue;
			}
		}

		if (tSpecifyDto is null)
		{
			candidateDto = default;
			return false;
		}

		var expectedLength = tSpecifyDto.DefaultTypeSyntaxes.Length;
		foreach (var overloadDto in overloadTypes.Value)
		{
			if (overloadDto.TypeSyntaxes.Length != expectedLength)
				throw new ArgumentException($"{TOverload.TagName} has a different count of template arguments than the {TSpecify.TagName}")
					.WithLocation(declarationSyntax);
		}

		candidateDto = new CandidateDto(
			declarationSyntax,
			tSpecifyDto,
			overloadTypes.Value,
			isInvertedMode,
			isTransitionsIgnored);
		return true;
	}
}
