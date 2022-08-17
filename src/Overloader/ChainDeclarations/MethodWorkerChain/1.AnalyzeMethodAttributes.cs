using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain;

internal sealed class AnalyzeMethodAttributes : IChainObj
{
	ChainResult IChainObj.Execute(GeneratorSourceBuilder gsb)
	{
		var entry = (MethodDeclarationSyntax) gsb.Entry;
		gsb.Store.ReturnType = entry.ReturnType.GetType(gsb.Compilation);
		gsb.Store.Modifiers = new string[entry.Modifiers.Count];
		for (int index = 0; index < gsb.Store.Modifiers.Length; index++)
			gsb.Store.Modifiers[index] = entry.Modifiers[index].ToString();

		foreach (var attrList in entry.AttributeLists)
		foreach (var attribute in attrList.Attributes)
		{
			string attrName = attribute.Name.GetName();
			if (attrName == AttributeNames.IgnoreForAttr)
			{
				if (attribute.ArgumentList is null or {Arguments.Count: < 1}) return ChainResult.BreakChain;
				foreach (var arg in attribute.ArgumentList.Arguments)
					if (arg.EqualsToTemplate(gsb))
						return ChainResult.BreakChain;
			}
			else if (attrName == AttributeNames.AllowForAttr)
			{
				if (attribute.ArgumentList is null or {Arguments.Count: < 1}) continue;
				foreach (var arg in attribute.ArgumentList.Arguments)
					if (!arg.EqualsToTemplate(gsb))
						return ChainResult.BreakChain;

				gsb.Store.MemberSkip = false;
			}
			else if (attrName == AttributeNames.TAttr)
			{
				switch (attribute.ArgumentList?.Arguments.Count ?? 0)
				{
					case 0 when gsb.Template is not null:
						gsb.Store.ReturnType = gsb.Template;
						gsb.Store.IsSmthChanged = true;
						break;
					case 0:
						break;
					case 1:
					case 2 when attribute.ArgumentList!.Arguments[1].EqualsToTemplate(gsb):
					{
						gsb.Store.ReturnType = attribute.ArgumentList!.Arguments[0].GetType(gsb.Compilation);
						gsb.Store.IsSmthChanged = true;
						break;
					}
					case 2 when gsb.Template is not null:
						var returnTypeSymbol = entry.ReturnType.GetType(gsb.Compilation);
						if (gsb.TryGetFormatter(returnTypeSymbol, out var formatter))
						{
							var @params = new ITypeSymbol[formatter.GenericParams.Length];

							for (int paramIndex = 0; paramIndex < formatter.GenericParams.Length; paramIndex++)
								@params[paramIndex] = formatter.GenericParams[paramIndex].GetType(gsb.Template) ?? throw new ArgumentException(
									$"Can't get type of formatter param (key: {returnTypeSymbol}) by index {paramIndex}.");

							var originalType = (INamedTypeSymbol) returnTypeSymbol.OriginalDefinition;
							gsb.Store.ReturnType = originalType.Construct(@params);
							gsb.Store.IsSmthChanged = true;
						}

						break;
					case 2 when gsb.Template is not null:
						gsb.Store.ReturnType = gsb.Template;
						gsb.Store.IsSmthChanged = true;
						break;
					case 2:
						break;
					default:
						throw new ArgumentException($"Unexpected count of arguments in {nameof(TAttribute)}.");
				}
			}
			else if (attrName == AttributeNames.ChangeModifierAttr)
			{
				if ((attribute.ArgumentList?.Arguments.Count ?? 0) <= 1)
					throw new ArgumentException($"Unexpected count of arguments in {nameof(ChangeModifierAttribute)}.");

				var arguments = attribute.ArgumentList!.Arguments;
				if (arguments.Count == 3 && !arguments[2].EqualsToTemplate(gsb)) continue;

				string modifier = arguments[0].Expression.GetInnerText();
				string newModifier = arguments[1].Expression.GetInnerText();

				for (int index = 0; index < gsb.Store.Modifiers.Length; index++)
				{
					if (!gsb.Store.Modifiers[index].Equals(modifier)) continue;

					gsb.Store.Modifiers[index] = newModifier;
					break;
				}
			}
		}

		return gsb.Store.MemberSkip ? ChainResult.BreakChain : ChainResult.NextChainMember;
	}
}
