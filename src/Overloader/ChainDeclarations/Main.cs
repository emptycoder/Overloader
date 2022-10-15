using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Entities;
using Overloader.Enums;
using Overloader.Utils;

namespace Overloader.ChainDeclarations;

internal class Main : IChainMember
{
	ChainAction IChainMember.Execute(GeneratorProperties props, SyntaxNode syntaxNode)
	{
		var sb = props.Builder;
		if (props.Template is null && !props.StartEntry.Syntax.Modifiers.Any(modifier => modifier.Text.Equals("partial")))
			return ChainAction.Break;

		var entrySyntax = props.StartEntry.Syntax;
		sb.AppendUsings(entrySyntax.GetTopParent())
			.AppendNamespace(entrySyntax.GetNamespace())
			.Append(string.Empty, 2);

		// Declare class/struct/record signature
		sb.Append(entrySyntax.AttributeLists.ToFullString(), 1)
			.AppendWith(entrySyntax.Modifiers.ToFullString(), " ")
			.AppendWith(entrySyntax.Keyword.ToFullString(), " ")
			.Append(props.ClassName, 1)
			.NestedIncrease(SyntaxKind.OpenBraceToken);

		foreach (var member in props.StartEntry.Syntax.Members)
		{
			if (member is not MethodDeclarationSyntax) continue;

			props.Store.MemberSkip = props.StartEntry.IsBlackListMode;
			foreach (var worker in Chains.MethodWorkers)
				if (worker.Execute(props, member) == ChainAction.Break)
					break;
		}

		sb.NestedDecrease(SyntaxKind.CloseBraceToken);

		return ChainAction.NextMember;
	}
}
