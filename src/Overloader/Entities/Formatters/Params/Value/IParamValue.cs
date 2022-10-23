using Microsoft.CodeAnalysis;

namespace Overloader.Entities.Formatters.Params.Value;

internal interface IParamValue
{
	ITypeSymbol GetType(ITypeSymbol template);
}
