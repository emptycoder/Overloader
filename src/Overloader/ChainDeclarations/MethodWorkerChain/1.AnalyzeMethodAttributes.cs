﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain;

internal sealed class AnalyzeMethodAttributes : IChainObj
{
	ChainResult IChainObj.Execute(GeneratorSourceBuilder gsb)
	{
		var entry = (MethodDeclarationSyntax) gsb.Entry;
		gsb.Store.IsSmthChanged = false;
		gsb.Store.FormattersWoIntegrityCount = 0;
		gsb.Store.ReturnType = entry.ReturnType.GetType(gsb.Compilation);
		gsb.Store.Modifiers = new string[entry.Modifiers.Count];
		for (int index = 0; index < gsb.Store.Modifiers.Length; index++)
			gsb.Store.Modifiers[index] = entry.Modifiers[index].ToString();

		foreach (var attrList in entry.AttributeLists)
		foreach (var attribute in attrList.Attributes)
		{
			string attrName = attribute.Name.GetName();
			if (attrName == Attributes.IgnoreForAttr)
			{
				if (attribute.ArgumentList is null or {Arguments.Count: < 1}) return ChainResult.BreakChain;
				foreach (var arg in attribute.ArgumentList.Arguments)
					if (arg.EqualsToTemplate(gsb))
						return ChainResult.BreakChain;
			}
			else if (attrName == Attributes.AllowForAttr)
			{
				if (attribute.ArgumentList is null or {Arguments.Count: < 1})
				{
					gsb.Store.MemberSkip = false;
					continue;
				}

				foreach (var arg in attribute.ArgumentList.Arguments)
					if (arg.EqualsToTemplate(gsb))
					{
						gsb.Store.MemberSkip = false;
						break;
					}
			}
			else if (attrName == Attributes.TAttr)
			{
				var returnTypeSymbol = entry.ReturnType.GetType(gsb.Compilation);
				var returnTypeSymbolRoot = returnTypeSymbol.GetRootType();
				switch (attribute.ArgumentList?.Arguments.Count ?? 0)
				{
					case 1:
					case 2 when attribute.ArgumentList!.Arguments[1].EqualsToTemplate(gsb):
					{
						gsb.Store.ReturnType = attribute.ArgumentList!.Arguments[0].GetType(gsb.Compilation);
						gsb.Store.IsSmthChanged = true;
						break;
					}
					case 2:
						break;
					case 0 when gsb.Template is null:
						break;
					case 0 when gsb.TryGetFormatter(returnTypeSymbolRoot, out var formatter):
						var @params = new ITypeSymbol[formatter.GenericParams.Length];

						for (int paramIndex = 0; paramIndex < formatter.GenericParams.Length; paramIndex++)
							@params[paramIndex] = formatter.GenericParams[paramIndex].GetType(gsb.Template) ?? throw new ArgumentException(
									$"Can't get type of formatter param (key: {returnTypeSymbol}) by index {paramIndex}.")
								.WithLocation(attribute.GetLocation());

						gsb.Store.ReturnType = returnTypeSymbol.SetRootType(
							returnTypeSymbolRoot
								.OriginalDefinition
								.Construct(@params),
							gsb.Compilation);
						gsb.Store.IsSmthChanged = true;
						break;
					case 0:
						gsb.Store.ReturnType = gsb.Template;
						gsb.Store.IsSmthChanged = true;
						break;
					default:
						throw new ArgumentException($"Unexpected count of arguments in {Attributes.TAttr}.")
							.WithLocation(attribute.GetLocation());
				}
			}
			else if (attrName == Attributes.ChangeModifierAttr)
			{
				if ((attribute.ArgumentList?.Arguments.Count ?? 0) <= 1)
					throw new ArgumentException($"Unexpected count of arguments in {Attributes.ChangeModifierAttr}.")
						.WithLocation(attribute.GetLocation());

				var arguments = attribute.ArgumentList!.Arguments;
				if (arguments.Count == 3 && !arguments[2].EqualsToTemplate(gsb)) continue;

				string modifier = arguments[0].Expression.GetInnerText();
				string newModifier = arguments[1].Expression.GetInnerText();

				for (int index = 0; index < gsb.Store.Modifiers.Length; index++)
				{
					if (!gsb.Store.Modifiers[index].Equals(modifier)) continue;

					gsb.Store.Modifiers[index] = newModifier;
					gsb.Store.IsSmthChanged = true;
					break;
				}
			}
		}

		return gsb.Store.MemberSkip ? ChainResult.BreakChain : ChainResult.NextChainMember;
	}
}
