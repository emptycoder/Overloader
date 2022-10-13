using Microsoft.CodeAnalysis;
using Overloader.Entities.Builders;
using Overloader.Entities.Formatters;

namespace Overloader.Entities;

internal class GeneratorProperties : IGeneratorProps, IDisposable
{
	public StoreDictionary Store { get; } = new();
	public SourceBuilder Builder { get; } = SourceBuilder.GetInstance();
	public Dictionary<ITypeSymbol, Formatter> Formatters { private get; init; } = default!;
	public Dictionary<ITypeSymbol, Formatter> GlobalFormatters { private get; init; } = default!;
	public GeneratorExecutionContext Context { private get; init; }
	public TypeEntrySyntax StartEntry { get; init; }
	public string ClassName { get; init; } = default!;
	public ITypeSymbol? Template { get; init; }

	public Compilation Compilation => Context.Compilation;

	public bool TryGetFormatter(ITypeSymbol type, out Formatter formatter)
	{
		if (Formatters.TryGetValue(type, out formatter)) return true;
		var originalType = type.OriginalDefinition;

		return Formatters.TryGetValue(originalType, out formatter) ||
		       GlobalFormatters.TryGetValue(type, out formatter) ||
		       GlobalFormatters.TryGetValue(originalType, out formatter);
	}

	public void ReleaseAsOutput()
	{
		lock (Context.Compilation)
		{
			int partialRev = 0;
			string source = Builder.ToString();
			AddLoop:
			try
			{
				partialRev++;
				Context.AddSource($"{ClassName}`{partialRev.ToString()}.g.cs", source);
			}
			catch { goto AddLoop; }
		}
	}
	
	void IDisposable.Dispose() => Builder.Dispose();
}

public interface IGeneratorProps
{
	public string ClassName { get; }
	public ITypeSymbol? Template { get; }
	public Compilation Compilation { get; }
}
