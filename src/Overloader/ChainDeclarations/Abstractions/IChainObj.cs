using Overloader.Entities;
using Overloader.Enums;

namespace Overloader.ChainDeclarations.Abstractions;

public interface IChainObj<T>
{
	public ChainResult Execute(GeneratorSourceBuilder<T> gsb);
}
