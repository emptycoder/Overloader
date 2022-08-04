using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.Abstractions;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Utils;

namespace Overloader.ChainDeclarations;

public class Main : IChainObj<TypeEntrySyntax>
{
	public ChainResult Execute(GeneratorSourceBuilder<TypeEntrySyntax> gsb)
	{
		gsb.AppendUsings(gsb.Entry.Syntax.GetTopParent().DescendantNodes().Where(node => node is UsingDirectiveSyntax))
			.Append($"namespace {gsb.Entry.Syntax.GetNamespace()};", 2);

		// Declare class/struct/record signature
		gsb.Append(gsb.Entry.Syntax.Modifiers.ToFullString(), 1, ' ')
			.Append(gsb.Entry.Syntax.Keyword.ToFullString(), 1, ' ')
			.AppendLineAndNestedIncrease(gsb.ClassName);

		foreach (var member in gsb.Entry.Syntax.Members)
		{
			if (member is not MethodDeclarationSyntax methodSyntax)
			{
				gsb.Append(member.ToFullString());
				continue;
			}

			foreach (var worker in Chains.MethodWorkers)
				if (worker.Execute(gsb.With(methodSyntax)) == ChainResult.BreakChain)
					break;
		}

		gsb.NestedDecrease();

		return ChainResult.NextChainMember;
	}
}
