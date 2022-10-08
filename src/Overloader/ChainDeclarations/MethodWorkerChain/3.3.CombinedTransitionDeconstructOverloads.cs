using Microsoft.CodeAnalysis;
using Overloader.Entities;
using Overloader.Enums;

namespace Overloader.ChainDeclarations.MethodWorkerChain;

internal sealed class CombinedTransitionDeconstructOverloads : IChainMember {
	ChainAction IChainMember.Execute(GeneratorProperties props, SyntaxNode syntaxNode)
	{
		return ChainAction.NextMember;
	}
}
