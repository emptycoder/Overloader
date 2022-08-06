using Overloader.Entities;
using Overloader.Enums;

namespace Overloader.ChainDeclarations.Abstractions;

internal interface IChainObj
{
	internal ChainResult Execute(GeneratorSourceBuilder gsb);
}
