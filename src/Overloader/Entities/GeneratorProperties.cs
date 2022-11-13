using Microsoft.CodeAnalysis;
using Overloader.Entities.Builders;
using Overloader.Entities.Formatters;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities;

internal record GeneratorProperties
	: IGeneratorProps, IDisposable
{
	private readonly Dictionary<ITypeSymbol, Formatter>? _formatters;
	public readonly bool IsTSpecified;

	public GeneratorExecutionContext Context;
	public TypeEntrySyntax StartEntry;

	public GeneratorProperties(
		GeneratorExecutionContext context,
		TypeEntrySyntax startEntry,
		bool isTSpecified,
		string className,
		Dictionary<ITypeSymbol, Formatter>? formatters,
		ITypeSymbol template)
	{
		Context = context;
		StartEntry = startEntry;
		IsTSpecified = isTSpecified;
		ClassName = className;
		Template = template;
		_formatters = formatters;

		if (_formatters is null) return;

		// Verify that all transitions have formatters
		foreach (var keyValuePair in _formatters)
		{
			foreach (var deconstructTransition in keyValuePair.Value.DeconstructTransitions.Span)
			foreach (var deconstructTransitionLink in deconstructTransition.Links)
			{
				var clearType = deconstructTransitionLink.TemplateType.GetClearType();
				if (clearType.IsGenericType && !TryGetFormatter(clearType, out _))
					throw new ArgumentException($"Can't get formatter for {ClassName}/{clearType.ToDisplayString()}.")
						.WithLocation(StartEntry.Syntax);
			}

			foreach (var integrityTransition in keyValuePair.Value.IntegrityTransitions.Span)
			{
				var clearType = integrityTransition.TemplateType.GetClearType();
				if (clearType.IsGenericType && !TryGetFormatter(clearType, out _))
					throw new ArgumentException($"Can't get formatter for {ClassName}/{clearType.ToDisplayString()}.")
						.WithLocation(StartEntry.Syntax);
			}
		}
	}

	public StoreDictionary Store { get; } = new();
	public SourceBuilder Builder { get; } = SourceBuilder.GetInstance();

	void IDisposable.Dispose() => Builder.Dispose();
	public string ClassName { get; }
	public ITypeSymbol Template { get; }

	// ReSharper disable once InconsistentlySynchronizedField
	public Compilation Compilation => Context.Compilation;

	public bool TryGetFormatter(ITypeSymbol type, out Formatter formatter)
	{
		if (_formatters is not null)
			return _formatters.TryGetValue(type, out formatter) ||
			       _formatters.TryGetValue(type.OriginalDefinition, out formatter);

		formatter = default!;
		return false;
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
}

public interface IGeneratorProps
{
	public string ClassName { get; }
	public ITypeSymbol? Template { get; }
	public Compilation Compilation { get; }
}
