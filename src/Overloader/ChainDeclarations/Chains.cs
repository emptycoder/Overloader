using Overloader.ChainDeclarations.Abstractions;
using Overloader.ChainDeclarations.MethodWorkerChain;

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
