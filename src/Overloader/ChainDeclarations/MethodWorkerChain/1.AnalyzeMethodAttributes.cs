using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.Abstractions;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Utils;

namespace Overloader.ChainDeclarations.MethodWorkerChain;

internal sealed class AnalyzeMethodAttributes : IChainObj
{
	ChainResult IChainObj.Execute(GeneratorSourceBuilder gsb)
	{
		var entry = (MethodDeclarationSyntax) gsb.Entry;
		// Analyze method attributes
		gsb.Store.ReturnType = entry.ReturnType;
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
			else if (attrName == AttributeNames.TAttr)
			{
				switch (attribute.ArgumentList?.Arguments.Count ?? 0)
				{
					case 0 when gsb.TemplateSyntax is not null:
						gsb.Store.ReturnType = gsb.TemplateSyntax;
						gsb.Store.IsSmthChanged = true;
						break;
					case 0:
						break;
					case 1:
					case 2 when attribute.ArgumentList!.Arguments[1].EqualsToTemplate(gsb):
					{
						gsb.Store.ReturnType = SyntaxFactory.ParseTypeName(
							attribute.ArgumentList!.Arguments[0].GetType(gsb.Compilation).Name);
						gsb.Store.IsSmthChanged = true;
						break;
					}
					default:
						throw new ArgumentException($"Unexpected count of arguments in {nameof(TAttribute)}.");
				}
			}
			// TODO: Analyze modifiers and create realization
			else if (attrName == AttributeNames.ChangeAccessModifierAttr) { }
		}

		return ChainResult.NextChainMember;
	}
}
