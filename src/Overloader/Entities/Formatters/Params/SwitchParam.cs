using Microsoft.CodeAnalysis;

namespace Overloader.Entities.Formatters.Params;

internal sealed class SwitchParam : IParam
{
	private readonly Dictionary<ITypeSymbol, IParam> _data;

	private SwitchParam(Dictionary<ITypeSymbol, IParam> data) => _data = data;
	ITypeSymbol IParam.GetType(ITypeSymbol template) => _data[template].GetType(template);

	public static SwitchParam Create(Dictionary<ITypeSymbol, IParam> data) => new(data);
}
