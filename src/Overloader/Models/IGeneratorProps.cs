using Microsoft.CodeAnalysis;

namespace Overloader.Models;

public interface IGeneratorProps
{
	public ITypeSymbol? Template { get; }
	public Compilation Compilation { get; }
}
