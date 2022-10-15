using Microsoft.CodeAnalysis;
using Overloader.ChainDeclarations.MethodWorkerChain;
using Overloader.Entities;
using Overloader.Enums;

namespace Overloader.ChainDeclarations;

public static class Chains
{
	internal static readonly IChainMember Main = new Main();

	internal static readonly IChainMember[] MethodWorkers =
	{
		/* 1 */ new AnalyzeMethodAttributes(),
		/* 2 */ new AnalyzeMethodParams(),
		/* 3 */ new DeconstructOverload(),
		/* 3.1 */ new CombinedDeconstructOverload(),
		/* 3.2 */ new TransitionDeconstructOverloads(),
		/* 3.3 */ new CombinedTransitionDeconstructOverloads(),
		/* 4 */ new IntegrityOverload(),
		/* 4.1 */ new CombinedIntegrityOverload()
	};
}

internal interface IChainMember
{
	internal ChainAction Execute(GeneratorProperties props, SyntaxNode syntaxNode);
}
