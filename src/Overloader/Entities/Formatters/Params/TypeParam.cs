using Microsoft.CodeAnalysis;

namespace Overloader.Entities.Formatters.Params;

internal sealed class TypeParam : IParam
{
	private readonly ITypeSymbol _typeSymbol;

	private TypeParam(ITypeSymbol typeSymbol) => _typeSymbol = typeSymbol;
	public string? Name { get; private init; }
	ITypeSymbol? IParam.GetType(ITypeSymbol? template) => _typeSymbol;

	public static TypeParam Create(ITypeSymbol typeSymbol, string? name = null) => new(typeSymbol) {Name = name};
}
