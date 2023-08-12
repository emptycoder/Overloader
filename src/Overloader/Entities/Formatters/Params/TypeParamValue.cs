using Microsoft.CodeAnalysis;

namespace Overloader.Entities.Formatters.Params;

public sealed class TypeParamValue : IParamValue
{
	private readonly ITypeSymbol _typeSymbol;

	private TypeParamValue(ITypeSymbol typeSymbol) => _typeSymbol = typeSymbol;
	ITypeSymbol IParamValue.GetType(ITypeSymbol template) => _typeSymbol;

	public static TypeParamValue Create(ITypeSymbol typeSymbol) => new(typeSymbol);
}
