using Microsoft.CodeAnalysis;

namespace Overloader.Entities.Formatters.Params;

internal sealed class TemplateParam : IParam
{
	private static readonly TemplateParam CachedTemplate = new();

	private TemplateParam() { }
	ITypeSymbol IParam.GetType(ITypeSymbol template) => template;

	public static TemplateParam Create() => CachedTemplate;
}
