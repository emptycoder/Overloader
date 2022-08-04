using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.ChainDeclarations.Abstractions;
using Overloader.ChainDeclarations.MethodWorkerChain;
using Overloader.Entities;

namespace Overloader.ChainDeclarations;

public class Chains
{
	public static readonly IChainObj<TypeEntrySyntax> Main = new Main();

	public static readonly IChainObj<MethodDeclarationSyntax>[] MethodWorkers =
	{
		new AnalyzeMethodAttributes(),
		new AnalyzeMethodParams()
	};
}
