using Microsoft.CodeAnalysis;

namespace Overloader.Formatters.Params.Value;

public sealed class SwitchParamValue : IParamValue
{
	private readonly Dictionary<ITypeSymbol, IParamValue> _data;

	private SwitchParamValue(Dictionary<ITypeSymbol, IParamValue> data) => _data = data;
	ITypeSymbol IParamValue.GetType(ITypeSymbol template) => _data[template].GetType(template);

	public static SwitchParamValue Create(Dictionary<ITypeSymbol, IParamValue> data) => new(data);
}
