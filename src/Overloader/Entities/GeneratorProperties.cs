using Microsoft.CodeAnalysis;
using Overloader.Entities.ContentBuilders;
using Overloader.Entities.DTOs;
using Overloader.Entities.Formatters;
using Overloader.Exceptions;
using Overloader.Utils;

namespace Overloader.Entities;

internal record GeneratorProperties
	: IGeneratorProps, IDisposable
{
	private static readonly Dictionary<ITypeSymbol, Formatter> Empty = new(0, SymbolEqualityComparer.Default);

	private readonly Dictionary<ITypeSymbol, Formatter> _formatters;
	private readonly Dictionary<ITypeSymbol, Formatter> _overloadFormatters;
	public readonly bool IsTSpecified;

	public GeneratorExecutionContext Context;
	public CandidateDto StartEntry;

	public Store Store { get; } = new();
	public SourceBuilder Builder { get; } = SourceBuilder.GetInstance();

	public string ClassName { get; }
	public ITypeSymbol Template { get; }

	// ReSharper disable once InconsistentlySynchronizedField
	public Compilation Compilation => Context.Compilation;

	public GeneratorProperties(
		GeneratorExecutionContext context,
		CandidateDto startEntry,
		Dictionary<ITypeSymbol, Formatter>? formatters,
		bool isTSpecified,
		string className,
		ITypeSymbol template,
		Dictionary<ITypeSymbol, Formatter>? overloadFormatters)
	{
		Context = context;
		StartEntry = startEntry;
		IsTSpecified = isTSpecified;
		ClassName = className;
		Template = template;
		_formatters = formatters ?? Empty;
		_overloadFormatters = overloadFormatters ?? Empty;

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

	public bool TryGetFormatter(ITypeSymbol type, out Formatter formatter) =>
		_overloadFormatters.TryGetValue(type, out formatter) || 
		_overloadFormatters.TryGetValue(type.OriginalDefinition, out formatter) ||
		_formatters.TryGetValue(type, out formatter) ||
		_formatters.TryGetValue(type.OriginalDefinition, out formatter);

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
	public ITypeSymbol? Template { get; }
	public Compilation Compilation { get; }
}
