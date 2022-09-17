using Microsoft.CodeAnalysis;

namespace Overloader.Entities.Formatters.Params;

internal sealed class TemplateParam : IParam
{
	private static readonly TemplateParam CachedTemplate = new();

	private TemplateParam() { }
	public string? Name { get; private init; }
	ITypeSymbol? IParam.GetType(ITypeSymbol? template) => template;

	public static TemplateParam Create(string? name = null) => name is null ? CachedTemplate : new TemplateParam {Name = name};
}
