using Microsoft.CodeAnalysis;

namespace Overloader.Formatters.Params;

internal sealed class SwitchParam : IParam
{
	private readonly Dictionary<ITypeSymbol, ITypeSymbol?> _data;

	private SwitchParam(Dictionary<ITypeSymbol, ITypeSymbol?> data) => _data = data;
	public string? Name { get; private init; }
	ITypeSymbol? IParam.GetType(ITypeSymbol? template) => template is null ? template : _data[template] ?? template;

	public static SwitchParam Create(Dictionary<ITypeSymbol, ITypeSymbol?> data, string? name = null) => new(data) {Name = name};
}
