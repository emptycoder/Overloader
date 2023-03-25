using Microsoft.CodeAnalysis;
using Overloader.ChainDeclarations.MethodWorkerChain;
using Overloader.Enums;
using Overloader.Models;

namespace Overloader.ChainDeclarations;

public static class ChainDeclarations
{
	public static readonly IChainMember Main = new MainChain();

	public static readonly IChainMember[] MethodWorkers =
	{
		/* 1 */ new AnalyzeMethodAttributes(),
		/* 2 */ new AnalyzeMethodParams(),
		/* 3 */ new DecompositionOverload(),
		/* 3.1 */ new CombinedDecompositionOverload(),
		/* 3.2 */ new TransitionDecompositionOverloads(),
		/* 3.3 */ new CombinedTransitionDecompositionOverloads(),
		/* 4 */ new IntegrityOverload(),
		/* 4.1 */ new CombinedIntegrityOverload(),
		/* 4.2 */ new TransitionIntegrityOverloads(),
		/* 4.3 */ new CombinedTransitionIntegrityOverloads()
	};
}

public interface IChainMember
{
	internal ChainAction Execute(GeneratorProperties props, SyntaxNode syntaxNode);
}
