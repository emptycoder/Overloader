using Overloader.Entities;
using Overloader.Enums;

namespace Overloader.ChainDeclarations.Abstractions;

public interface IChainObj
{
	public ChainResult Execute(GeneratorSourceBuilder gsb);
}
