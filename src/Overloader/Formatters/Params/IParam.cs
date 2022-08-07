using Microsoft.CodeAnalysis;

namespace Overloader.Formatters.Params;

internal interface IParam
{
	string? Name { get; }
	ITypeSymbol? GetType(ITypeSymbol? template);
}
