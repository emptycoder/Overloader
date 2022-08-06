using Overloader.ChainDeclarations.Abstractions;
using Overloader.ChainDeclarations.MethodWorkerChain;

namespace Overloader.ChainDeclarations;

public class Chains
{
	public static readonly IChainObj Main = new Main();

	public static readonly IChainObj[] MethodWorkers =
	{
		new AnalyzeMethodAttributes(),
		new AnalyzeMethodParams(),
		new GenerateFormatterOverloads(),
		new GenerateTypeOverloads()
	};
}
