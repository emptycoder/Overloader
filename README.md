<h1 align="center">Overloader</h1>

Overloader is the open-source source generator that provide unsafe generics.
The main target of this project helps to write unsafe generics which don't have any common based interface or type.
Also, create method parameters overloads that helps improve developer experience.

# Installation

[NuGet](https://www.nuget.org/packages/Overloader/): `dotnet add package overloader`

# Specific types on generics

## Uncompilable code

```csharp
public static class GenericMath
{
	public static T Square<T>(T val) where T : double, float => val * val;
}
```

## Solution

```csharp
[TSpecify(typeof(double))]
[Overload(typeof(float), "D", "F")]
public static class GenericMathD
{
	public static double Square([T] double val) => val * val;
}
```

## Generated part

```csharp
public static partial class GenericMathF
{
	public static double Square(float val) => val * val;
}
```

P.S. GenericMath provided in preview versions of .NET try to resolve this problem, but we can't restrict needed types.

# Parameter overload creation to avoid additional struct/class allocation

## User template

```csharp
[Formatter(
	"Vector2",
	typeof(Vector2<>),
	new object[] {"T"},
	new object[]
	{
		"X", "T",
		"Y", "T"
	})]
[TSpecify(typeof(double), "Vector2")]
[Overload(typeof(float), "2D", "2F")]
public static partial class Vector2DExtension
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[return: T]
	public static ref Vector2<double> Sum([Integrity][T] this ref Vector2<double> vec1, [T] in Vector2<double> vec2)
	{
		vec1.X += vec2.X;
		vec1.Y += vec2.Y;

		return ref vec1;
	}
}
```

## Generated part

```csharp
public static partial class Vector2FExtension
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref Overloader.Examples.Vector2<float> Sum(this ref Overloader.Examples.Vector2<float> vec1, float vec2X, float vec2Y)
	{
		vec1.X += vec2X;
		vec1.Y += vec2Y;
		return ref vec1;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref Overloader.Examples.Vector2<float> Sum(this ref Overloader.Examples.Vector2<float> vec1, in Overloader.Examples.Vector2<float> vec2)
	{
		vec1.X += vec2.X;
		vec1.Y += vec2.Y;
		return ref vec1;
	}
}
```

# License

Overloader is licensed under the [MIT](./LICENSE) license.
