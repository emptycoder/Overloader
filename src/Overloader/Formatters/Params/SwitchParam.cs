using Microsoft.CodeAnalysis;

namespace Overloader.Formatters.Params;

internal sealed class SwitchParam : IParam
{
	private readonly Dictionary<ITypeSymbol, ITypeSymbol> _data;
	public string? Name { get; private init; }
		
	private SwitchParam(Dictionary<ITypeSymbol, ITypeSymbol> data) => _data = data;
	ITypeSymbol? IParam.GetType(ITypeSymbol? template) => template is null? template : _data[template];
		
	public static SwitchParam Create(Dictionary<ITypeSymbol, ITypeSymbol> data, string? name = null) => new(data) { Name = name };
}
