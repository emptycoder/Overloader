using Microsoft.CodeAnalysis;

namespace Overloader.Utils;

// ReSharper disable once InconsistentNaming
internal static class ITypeSymbolExtensions
{
	public static ITypeSymbol SetRootType(this ITypeSymbol typeSymbol, ITypeSymbol newRootType, Compilation compilation) =>
		typeSymbol switch
		{
			IArrayTypeSymbol arrayTypeSymbol => compilation.CreateArrayTypeSymbol(
				arrayTypeSymbol.ElementType.SetRootType(newRootType, compilation)),
			IPointerTypeSymbol pointerTypeSymbol => compilation.CreatePointerTypeSymbol(
				pointerTypeSymbol.PointedAtType.SetRootType(newRootType, compilation)),
			_ => newRootType
		};

	public static INamedTypeSymbol GetRootType(this ITypeSymbol typeSymbol)
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
