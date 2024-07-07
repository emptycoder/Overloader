using Microsoft.CodeAnalysis;

namespace Overloader.Entities.DTOs.Attributes.Formatters.Params;

public interface IParamValue
{
	ITypeSymbol GetType(ITypeSymbol template);
}
