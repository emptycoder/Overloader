using Microsoft.CodeAnalysis;

namespace Overloader.Entities.Formatters.Params;

internal interface IParam
{
	ITypeSymbol? GetType(ITypeSymbol? template);
}
