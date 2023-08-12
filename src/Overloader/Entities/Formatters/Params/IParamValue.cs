using Microsoft.CodeAnalysis;

namespace Overloader.Entities.Formatters.Params;

public interface IParamValue
{
	ITypeSymbol GetType(ITypeSymbol template);
}
