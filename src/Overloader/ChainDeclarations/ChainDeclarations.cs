using Microsoft.CodeAnalysis;
using Overloader.ChainDeclarations.Overloads;
using Overloader.Entities;
using Overloader.Enums;

namespace Overloader.ChainDeclarations;

public static class ChainDeclarations
{
	public static readonly IChainMember Main = new MainChain();
	public static readonly IChainMember[] MethodWorkers =
	{
		/* 1. Analyze stage */
		/* 1.0 */ new AnalyzeMethodAttributes(),
		/* 1.1 */ new AnalyzeMethodParams(),
		/* 2. Decomposition stage */
		/* 2.0 */ new DecompositionOverload(),
		/* 2.1 */ new CombinedDecompositionOverload(),
		/* 3. Integrity stage */
		/* 3.0 */ new IntegrityOverload(),
		/* 3.1 */ new CombinedIntegrityOverload(),
		/* 3.2 */ new RefIntegrityOverloads(),
		/* 3.3 */ new CombinedRefIntegrityOverloads(),
		/* 4. Decomposition transition stage */
		/* 4.0 */ new DecompositionTransitionOverloads(),
		/* 4.1 */ new CombinedDecompositionTransitionOverloads(),
		/* 5. Cast transition stage */
		/* 5.0 */ new CastTransitionOverloads(),
		/* 5.1 */ new CombinedCastTransitionOverloads(),
		/* 6. Cast for decomposition transition stage */
		/* 6.0 */ new CastForDTOverloads(),
		/* 6.1 */ new CombinedCastForDTOverloads(),
		/* 7. Cast for integrity transition stage */
		/* 7.0 */
		/* 7.1 */
	};
}

public interface IChainMember
{
	internal ChainAction Execute(GeneratorProperties props, SyntaxNode syntaxNode);
}
