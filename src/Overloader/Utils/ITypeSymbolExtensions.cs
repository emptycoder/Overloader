using Microsoft.CodeAnalysis;

namespace Overloader.Utils;

// ReSharper disable once InconsistentNaming
internal static class ITypeSymbolExtensions
{
	public static ITypeSymbol ConstructWithClearType(
		this ITypeSymbol typeSymbol,
		ITypeSymbol newClearType,
		Compilation compilation) =>
		typeSymbol switch
		{
			IArrayTypeSymbol arrayTypeSymbol => compilation.CreateArrayTypeSymbol(
				arrayTypeSymbol.ElementType.ConstructWithClearType(newClearType, compilation)),
			IPointerTypeSymbol pointerTypeSymbol => compilation.CreatePointerTypeSymbol(
				pointerTypeSymbol.PointedAtType.ConstructWithClearType(newClearType, compilation)),
			_ => newClearType
		};

	public static INamedTypeSymbol GetClearType(this ITypeSymbol typeSymbol)
	{
		while (true)
		{
			switch (typeSymbol)
			{
				case IArrayTypeSymbol arrayTypeSymbol:
					typeSymbol = arrayTypeSymbol.ElementType;
					continue;
				case IPointerTypeSymbol pointerTypeSymbol:
					typeSymbol = pointerTypeSymbol.PointedAtType;
					continue;
				case INamedTypeSymbol namedTypeSymbol:
					return namedTypeSymbol;
			}

			throw new ArgumentException($"Not supported {nameof(typeSymbol)}: {typeSymbol.ToDisplayString()}");
		}
	}
}
