using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Utils;

namespace Overloader.ChainDeclarations;

internal class Main : IChainObj
{
	ChainResult IChainObj.Execute(GeneratorProperties props, SyntaxNode syntaxNode)
	{
		var sb = props.Builder;
		if (props.Template is null && !props.StartEntry.Syntax.Modifiers.Any(modifier => modifier.Text.Equals("partial")))
			return ChainResult.BreakChain;

		sb.AppendUsings(props.StartEntry.Syntax.GetTopParent())
			.AppendWith("namespace", " ")
			.AppendWith(props.StartEntry.Syntax.GetNamespace(), ";")
			.Append(string.Empty, 2);

		// Declare class/struct/record signature
		sb.Append(props.StartEntry.Syntax.AttributeLists.ToFullString(), 1)
			.AppendWith(props.StartEntry.Syntax.Modifiers.ToFullString(), " ")
			.AppendWith(props.StartEntry.Syntax.Keyword.ToFullString(), " ")
			.Append(props.ClassName, 1)
			.NestedIncrease();

		foreach (var member in props.StartEntry.Syntax.Members)
		{
			if (member is not MethodDeclarationSyntax) continue;

			props.Store.MemberSkip = props.StartEntry.IsBlackListMode;
			foreach (var worker in Chains.MethodWorkers)
				if (worker.Execute(props, member) == ChainResult.BreakChain)
					break;
		}

		sb.NestedDecrease();

		return ChainResult.NextChainMember;
	}
}
