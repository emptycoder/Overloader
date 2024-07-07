using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities.Attributes;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities.DTOs;

public struct ParameterDto
{
	public TAttributeDto? Attribute;
	public bool HasForceOverloadIntegrity;
	public string? CombineWithParameter;
	public List<ModifierDto> ModifierChangers;

	public static bool TryGetParameterDtoByTemplate(ParameterSyntax syntaxNode,
		IGeneratorProps props,
		out ParameterDto parameterDto)
	{
		parameterDto = new ParameterDto
		{
			ModifierChangers = []
		};
		foreach (var attrList in syntaxNode.AttributeLists)
		foreach (var attribute in attrList.Attributes)
		{
			string attrName = attribute.Name.GetName();
			switch (attrName)
			{
				case Integrity.TagName:
					parameterDto.HasForceOverloadIntegrity = true;
					continue;
				case CombineWith.TagName when attribute.ArgumentList is {Arguments: var args}:
					if (args.Count != 1)
						throw new ArgumentException("Not allowed with arguments count != 1.")
							.WithLocation(syntaxNode);
					parameterDto.CombineWithParameter = args[0].Expression.GetVariableName();
					continue;
				case TAttribute.TagName:
					var tAttrDto = TAttributeDto.Parse(attribute, props.Compilation);
					if (tAttrDto.TemplateIndex >= props.Templates.Length)
						throw new ArgumentException("Template index for parameter is out of bounds")
							.WithLocation(attribute);
					
					if (SymbolEqualityComparer.Default.Equals(tAttrDto.TemplateTypeFor, props.Templates[tAttrDto.TemplateIndex])
							|| tAttrDto.TemplateTypeFor is null)
					{
						parameterDto.Attribute = tAttrDto;
					}
					continue;
				case Modifier.TagName:
					var modifierDto = ModifierDto.Parse(attribute, props.Compilation);
					if (modifierDto.TemplateTypeFor is not null
					    && !SymbolEqualityComparer.Default.Equals(modifierDto.TemplateTypeFor, props.Templates[modifierDto.TemplateIndexFor])) continue;

					parameterDto.ModifierChangers.Add(modifierDto);
					continue;
			}
		}

		return parameterDto.Attribute != null;
	}
}
