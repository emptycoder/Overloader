using Microsoft.CodeAnalysis;
using Overloader.Entities;
using Overloader.Entities.Attributes;
using Overloader.Enums;
using Overloader.Utils;

namespace Overloader.Chains.Overloads;

public sealed class AnalyzeMethodAttributes : IChainMember
{
	ChainAction IChainMember.Execute(GeneratorProperties props)
	{
		props.Store.IsSmthChanged = false;
		props.Store.MethodData.ReturnType = props.Store.MethodSyntax.ReturnType.GetType(props.Compilation);
		props.Store.MethodData.MethodModifiers = new string[props.Store.MethodSyntax.Modifiers.Count];
		props.Store.MethodData.MethodName = props.Store.MethodSyntax.Identifier.ToString();

		for (int index = 0; index < props.Store.MethodData.MethodModifiers.Length; index++)
			props.Store.MethodData.MethodModifiers[index] = props.Store.MethodSyntax.Modifiers[index].ToString();

		bool isAllowForAttrSet = false;
		foreach (var attrList in props.Store.MethodSyntax.AttributeLists)
		foreach (var attribute in attrList.Attributes)
		{
			string attrName = attribute.Name.GetName();
			switch (attrName)
			{
				case SkipMode.TagName:
					var skipDto = SkipModeDto.Parse(attribute, props.Compilation);
					if (skipDto.TemplateTypeFor is null || SymbolEqualityComparer.Default.Equals(skipDto.TemplateTypeFor, props.Templates[skipDto.TemplateIndexFor]))
					{
						if (isAllowForAttrSet && !props.Store.SkipMember) continue;
						
						props.Store.SkipMember = skipDto.ShouldBeSkipped;
						if (skipDto.ShouldBeSkipped) continue;
						isAllowForAttrSet = true;
					}
					break;
				case TAttribute.TagName:
					var returnTypeSymbol = props.Store.MethodSyntax.ReturnType.GetType(props.Compilation);
					var returnTypeSymbolRoot = returnTypeSymbol.GetClearType();
					var tAttrDto = TAttributeDto.Parse(attribute, props.Compilation);

					if (SymbolEqualityComparer.Default.Equals(tAttrDto.ForType, props.Templates[tAttrDto.TemplateIndex])
					    || (tAttrDto.ForType is null && tAttrDto.NewType is not null))
					{
						props.Store.MethodData.ReturnType = tAttrDto.NewType;
						props.Store.IsSmthChanged = true;
					}
					else if (props.TryGetFormatter(returnTypeSymbolRoot, out var formatter))
					{
						var @params = new ITypeSymbol[formatter.GenericParams.Length];

						for (int paramIndex = 0; paramIndex < formatter.GenericParams.Length; paramIndex++)
							@params[paramIndex] = formatter.GenericParams[paramIndex].GetType(props.Templates[tAttrDto.TemplateIndex]);

						props.Store.MethodData.ReturnType = returnTypeSymbol.ConstructWithClearType(
							returnTypeSymbolRoot
								.OriginalDefinition
								.Construct(@params),
							props.Compilation);
						props.Store.IsSmthChanged = true;
					}
					else
					{
						props.Store.MethodData.ReturnType = props.Templates[tAttrDto.TemplateIndex];
					}
					break;
				case ChangeModifier.TagName:
					var changeModifierDto = ChangeModifierDto.Parse(attribute, props.Compilation);
					if (changeModifierDto.TemplateTypeFor is not null
					    && !SymbolEqualityComparer.Default.Equals(changeModifierDto.TemplateTypeFor, props.Templates[changeModifierDto.TemplateIndexFor])) continue;

					for (int index = 0; index < props.Store.MethodData.MethodModifiers.Length; index++)
					{
						if (!props.Store.MethodData.MethodModifiers[index].Equals(changeModifierDto.Modifier)) continue;

						props.Store.MethodData.MethodModifiers[index] = changeModifierDto.NewModifier;
						props.Store.IsSmthChanged = true;
						break;
					}
					break;
				case ChangeName.TagName when !props.IsDefaultOverload:
					var changeNameDto = ChangeNameDto.Parse(attribute, props.Compilation);
					if (changeNameDto.TemplateTypeFor is not null
					    && !SymbolEqualityComparer.Default.Equals(changeNameDto.TemplateTypeFor, props.Templates[changeNameDto.TemplateIndexFor])) continue;
					
					props.Store.MethodData.MethodName = changeNameDto.NewName;
					props.Store.IsSmthChanged = true;
					break;
				case ForceChanged.TagName:
					props.Store.IsSmthChanged = true;
					break;
			}
		}

		return props.Store.SkipMember ? ChainAction.Break : ChainAction.NextMember;
	}
}
