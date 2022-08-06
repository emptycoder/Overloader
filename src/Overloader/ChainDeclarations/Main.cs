using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.Abstractions;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Utils;

namespace Overloader.ChainDeclarations;

public class Main : IChainObj
{
	public ChainResult Execute(GeneratorSourceBuilder gsb)
	{
		var entry = (TypeEntrySyntax) gsb.Entry;
		gsb.AppendUsings(entry.Syntax.GetTopParent().DescendantNodes().Where(node => node is UsingDirectiveSyntax))
			.Append($"namespace {entry.Syntax.GetNamespace()};", 2);

		// Declare class/struct/record signature
		gsb.Append(entry.Syntax.Modifiers.ToFullString(), 1, ' ')
			.Append(entry.Syntax.Keyword.ToFullString(), 1, ' ')
			.AppendLineAndNestedIncrease(gsb.ClassName);

		foreach (var member in entry.Syntax.Members)
		{
			if (member is not MethodDeclarationSyntax methodSyntax)
			{
				gsb.Append(member.ToFullString());
				continue;
			}

			foreach (var worker in Chains.MethodWorkers)
				if (worker.Execute(gsb with {Entry = methodSyntax}) == ChainResult.BreakChain)
					break;
		}

		gsb.NestedDecrease();

		return ChainResult.NextChainMember;
	}
}
