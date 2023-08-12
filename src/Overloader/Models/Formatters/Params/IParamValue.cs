using Microsoft.CodeAnalysis;

namespace Overloader.Models.Formatters.Params;

public interface IParamValue
{
	ITypeSymbol GetType(ITypeSymbol template);
}
