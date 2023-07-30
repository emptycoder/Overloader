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
		/* 3.2 */ new DecompositionTransitionOverloads(),
		/* 3.3 */ new CombinedDecompositionTransitionOverloads(),
		/* 4 */ new IntegrityOverload(),
		/* 4.1 */ new CombinedIntegrityOverload(),
		/* 4.2 */ new CastTransitionOverloads(),
		/* 4.3 */ new CombinedCastTransitionOverloads(),
		/* 5.0 */ new RefIntegrityOverloads(),
		/* 5.1 */ new CombinedRefIntegrityOverloads()
	};
}

public interface IChainMember
{
	internal ChainAction Execute(GeneratorProperties props, SyntaxNode syntaxNode);
}
