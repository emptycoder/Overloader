using Microsoft.CodeAnalysis;

namespace Overloader.Entities.Formatters.Params;

internal interface IParam
{
	string? Name { get; }
	ITypeSymbol? GetType(ITypeSymbol? template);
}
