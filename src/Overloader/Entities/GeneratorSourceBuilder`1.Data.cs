using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Formatters;

namespace Overloader.Entities;

internal partial record GeneratorSourceBuilder : IGeneratorProps
{
	private TypeSyntax? _typeSyntax;

	public StoreDictionary Store { get; } = new();
	public Dictionary<ITypeSymbol, Formatter> Formatters { private get; init; } = default!;
	public Dictionary<ITypeSymbol, Formatter> GlobalFormatters { private get; init; } = default!;
	public GeneratorExecutionContext Context { private get; init; }
	public object Entry { get; init; } = default!;
	public string ClassName { get; init; } = default!;
	public ITypeSymbol? Template { get; init; }

	public Compilation Compilation => Context.Compilation;

	public TypeSyntax? TemplateSyntax => _typeSyntax ??=
		Template is not null ? SyntaxFactory.ParseTypeName(Template.Name) : default;

	public bool TryGetFormatter(ITypeSymbol type, out Formatter formatter)
	{
		if (Formatters.TryGetValue(type, out formatter)) return true;
		var originalType = type.OriginalDefinition;

		return Formatters.TryGetValue(originalType, out formatter) ||
		       GlobalFormatters.TryGetValue(type, out formatter) ||
		       GlobalFormatters.TryGetValue(originalType, out formatter);
	}

	public void AddToContext()
	{
		int partialRev = 0;
		string source = ToString();
		AddLoop:
		try
		{
			partialRev++;
			Context.AddSource($"{ClassName}`{partialRev.ToString()}.g.cs", source);
		}
		catch { goto AddLoop; }
	}
}

public interface IGeneratorProps
{
	public string ClassName { get; }
	public ITypeSymbol? Template { get; }
	public TypeSyntax? TemplateSyntax { get; }
	public Compilation Compilation { get; }
}
