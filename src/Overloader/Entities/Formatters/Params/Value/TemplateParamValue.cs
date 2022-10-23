using Microsoft.CodeAnalysis;

namespace Overloader.Entities.Formatters.Params.Value;

internal sealed class TemplateParamValue : IParamValue
{
	private static readonly TemplateParamValue CachedTemplate = new();

	private TemplateParamValue() { }
	ITypeSymbol IParamValue.GetType(ITypeSymbol template) => template;

	public static TemplateParamValue Create() => CachedTemplate;
}
