using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Enums;
using Overloader.Models;
using Overloader.Utils;

namespace Overloader.ChainDeclarations;

internal class MainChain : IChainMember
{
	ChainAction IChainMember.Execute(GeneratorProperties props, SyntaxNode syntaxNode)
	{
		var sb = props.Builder;
		switch (props.IsTSpecified)
		{
			case true when props.StartEntry.IgnoreTransitions:
			case true when !props.StartEntry.Syntax.Modifiers.Any(SyntaxKind.PartialKeyword):
				return ChainAction.Break;
		}

		var entrySyntax = props.StartEntry.Syntax;
		if (entrySyntax.AttributeLists.Any(attrList => attrList.Attributes.Any(attr =>
			    attr.Name.GetName() == Constants.RemoveBodyAttr)))
			props.Store.IsNeedToRemoveBody = true;

		sb.AppendUsings(entrySyntax.GetTopParent())
			.AppendNamespace(entrySyntax.GetNamespace())
			.Append(string.Empty, 2);

		// Declare class/struct/record signature
		sb.AppendAttributes(entrySyntax.AttributeLists, "\n")
			.AppendWith(entrySyntax.Modifiers.ToString(), " ")
			.AppendWith(entrySyntax.Keyword.ToFullString(), " ")
			.Append(props.ClassName)
			.Append(entrySyntax.BaseList?.ToFullString() ?? string.Empty)
			.AppendWith(entrySyntax.TypeParameterList?.ToString() ?? string.Empty, " ")
			.Append(entrySyntax.ConstraintClauses.ToFullString(), 1)
			.NestedIncrease(SyntaxKind.OpenBraceToken);

		foreach (var member in props.StartEntry.Syntax.Members)
		{
			if (member is not MethodDeclarationSyntax) continue;

			props.Store.SkipMember = props.StartEntry.IsBlackListMode;
			foreach (var worker in ChainDeclarations.MethodWorkers)
				if (worker.Execute(props, member) == ChainAction.Break)
					break;
		}

		sb.NestedDecrease(SyntaxKind.CloseBraceToken);

		return ChainAction.NextMember;
	}
}
