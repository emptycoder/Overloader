using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Utils;

namespace Overloader.ChainDeclarations;

internal class Main : IChainObj
{
	ChainResult IChainObj.Execute(GeneratorSourceBuilder gsb)
	{
		var entry = (TypeEntrySyntax) gsb.Entry;
		gsb.AppendUsings(entry.Syntax.GetTopParent())
			.AppendWith("namespace", " ")
			.AppendWith(entry.Syntax.GetNamespace(), ";")
			.Append(string.Empty, 2);

		// Declare class/struct/record signature
		gsb.Append(entry.Syntax.AttributeLists.ToFullString(), 1);
		foreach (var modifier in entry.Syntax.Modifiers)
		{
			var modifierText = modifier.Text;
			if (modifierText.Equals("partial"))
				gsb.Store.IsPartial = true;
			gsb.AppendWith(modifierText, " ");
		}
		gsb.AppendWith(entry.Syntax.Keyword.ToFullString(), " ")
			.Append(gsb.ClassName, 1)
			.NestedIncrease();
		
		foreach (var member in entry.Syntax.Members)
		{
			if (member is not MethodDeclarationSyntax methodSyntax) continue;

			gsb.Store.MemberSkip = entry.IsBlackListMode;
			foreach (var worker in Chains.MethodWorkers)
				if (worker.Execute(gsb with {Entry = methodSyntax}) == ChainResult.BreakChain)
					break;
		}

		gsb.NestedDecrease();

		return ChainResult.NextChainMember;
	}
}
