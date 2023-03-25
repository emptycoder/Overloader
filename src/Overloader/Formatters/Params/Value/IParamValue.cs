using Microsoft.CodeAnalysis;

namespace Overloader.Formatters.Params.Value;

public interface IParamValue
{
	ITypeSymbol GetType(ITypeSymbol template);
}
