using Microsoft.CodeAnalysis;

namespace Overloader.Formatters.Params;

internal sealed class TemplateParam : IParam
{
	private static readonly TemplateParam CachedTemplate = new();
	public string? Name { get; private init; }
		
	private TemplateParam() {}
	ITypeSymbol? IParam.GetType(ITypeSymbol? template) => template;
		
	public static TemplateParam Create(string? name = null) => name is null? CachedTemplate : new TemplateParam { Name = name };
}
