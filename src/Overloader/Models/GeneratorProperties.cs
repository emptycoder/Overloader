using Microsoft.CodeAnalysis;
using Overloader.ContentBuilders;
using Overloader.DTOs;
using Overloader.Exceptions;
using Overloader.Models.Formatters;
using Overloader.Utils;

namespace Overloader.Models;

public record GeneratorProperties : IGeneratorProps, IDisposable
{
	private static readonly Dictionary<ITypeSymbol, FormatterModel> Empty = new(0, SymbolEqualityComparer.Default);

	private readonly Dictionary<ITypeSymbol, FormatterModel> _formatters;
	private readonly Dictionary<ITypeSymbol, FormatterModel> _overloadFormatters;
	public readonly bool IsTSpecified;

	public GeneratorExecutionContext Context;
	public CandidateDto StartEntry;

	public GeneratorProperties(
		GeneratorExecutionContext context,
		CandidateDto startEntry,
		Dictionary<ITypeSymbol, FormatterModel>? formatters,
		bool isTSpecified,
		string className,
		ITypeSymbol template,
		Dictionary<ITypeSymbol, FormatterModel>? overloadFormatters)
	{
		Context = context;
		StartEntry = startEntry;
		IsTSpecified = isTSpecified;
		ClassName = className;
		Template = template;
		_formatters = formatters ?? Empty;
		_overloadFormatters = overloadFormatters ?? Empty;

		VerifyFormatters(_formatters);
		VerifyFormatters(_overloadFormatters);

		void VerifyFormatters(Dictionary<ITypeSymbol, FormatterModel> formattersToCheck)
		{
			// Verify that all transitions have formatters
			foreach (var keyValuePair in formattersToCheck)
			{
				var formatter = keyValuePair.Value;
				foreach (var decompositionTransition in formatter.Decompositions)
				foreach (var decompositionTransitionLink in decompositionTransition.Links)
				{
					var clearType = decompositionTransitionLink.TemplateType.GetClearType();
					if (clearType.IsGenericType && !TryGetFormatter(clearType, out _))
						throw new ArgumentException($"Can't get formatter for {ClassName}/{clearType.ToDisplayString()}.")
							.WithLocation(StartEntry.Syntax);
				}

				foreach (var castTransition in formatter.Casts)
				foreach (var castTemplate in castTransition.Types)
				{
					var clearType = castTemplate.IsUnboundType
						? (INamedTypeSymbol) castTemplate.Type
						: castTemplate.Type.GetClearType();
					
					
					if (clearType.IsGenericType && !TryGetFormatter(clearType, out _))
						throw new ArgumentException($"Can't get formatter for {ClassName}/{clearType.ToDisplayString()}.")
							.WithLocation(StartEntry.Syntax);
				}
			}
		}
	}

	public Store Store { get; } = new();
	public SourceBuilder Builder { get; } = SourceBuilder.GetInstance();

	public string ClassName { get; }

	void IDisposable.Dispose() => Builder.Dispose();
	public ITypeSymbol Template { get; }

	// ReSharper disable once InconsistentlySynchronizedField
	public Compilation Compilation => Context.Compilation;

	public bool TryGetFormatter(ITypeSymbol type, out FormatterModel formatter) =>
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
}
