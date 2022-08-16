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
				// TODO:
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
			// TODO: Analyze modifiers and create realization
			else if (attrName == AttributeNames.ChangeAccessModifierAttr) { }
		}

		return gsb.Store.MemberSkip ? ChainResult.BreakChain : ChainResult.NextChainMember;
	}
}
