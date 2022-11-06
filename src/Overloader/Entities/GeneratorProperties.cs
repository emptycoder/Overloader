using Microsoft.CodeAnalysis;
using Overloader.Entities.Builders;
using Overloader.Entities.Formatters;

namespace Overloader.Entities;

internal class GeneratorProperties : IGeneratorProps, IDisposable
{
	public StoreDictionary Store { get; } = new();
	public SourceBuilder Builder { get; } = SourceBuilder.GetInstance();
	public Dictionary<ITypeSymbol, Formatter> Formatters { private get; init; } = default!;
	public GeneratorExecutionContext Context { private get; init; }
	public TypeEntrySyntax StartEntry { get; init; }
	public bool IsTSpecified { get; init; }

	void IDisposable.Dispose() => Builder.Dispose();
	public string ClassName { get; init; } = default!;
	public ITypeSymbol Template { get; init; } = default!;

	public Compilation Compilation => Context.Compilation;

	public bool TryGetFormatter(ITypeSymbol type, out Formatter formatter) =>
		Formatters.TryGetValue(type, out formatter) ||
		Formatters.TryGetValue(type.OriginalDefinition, out formatter);

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
}

public interface IGeneratorProps
{
	public string ClassName { get; }
	public ITypeSymbol? Template { get; }
	public Compilation Compilation { get; }
}
