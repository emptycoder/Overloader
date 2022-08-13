using Overloader.ChainDeclarations.MethodWorkerChain;
using Overloader.Entities;
using Overloader.Enums;

namespace Overloader.ChainDeclarations;

public static class Chains
{
	internal static readonly IChainObj Main = new Main();

	internal static readonly IChainObj[] MethodWorkers =
	{
		new AnalyzeMethodAttributes(),
		new AnalyzeMethodParams(),
		new GenerateFormatterOverloads(),
		new GenerateTypeOverloads()
	};
}

internal interface IChainObj
{
	internal ChainResult Execute(GeneratorSourceBuilder gsb);
}
