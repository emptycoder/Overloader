using Microsoft.CodeAnalysis;

namespace Overloader.Formatters.Params;

internal sealed class TypeParam : IParam
{
	private readonly ITypeSymbol _typeSymbol;
	public string? Name { get; private init; }
		
	private TypeParam(ITypeSymbol typeSymbol) => _typeSymbol = typeSymbol;
	ITypeSymbol? IParam.GetType(ITypeSymbol? template) => _typeSymbol;
		
	public static TypeParam Create(ITypeSymbol typeSymbol, string? name = null) => new(typeSymbol) { Name = name };
}
