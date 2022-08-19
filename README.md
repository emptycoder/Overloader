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
[Overload(typeof(float), "D", "F")]
public static class GenericMathD
{
	public static double Square([T] double val) => val * val;
}
```

## Generated part

```csharp
[Overload(typeof(float), "D", "F")]
public static partial class GenericMathF
{
	public static double Square([T] float val) => val * val;
}
```

P.S. GenericMath provided in preview versions of .net try to resolve this problem, but we can't restrict needed types.

# Parameter overload creation to avoid additional struct/class allocation

## User template

```csharp
[Formatter(typeof(Vector2<>),
	new object[] {"T"},
	new object[]
	{
		"X", "T",
		"Y", "T"
	})]
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
[Overload(typeof(float), "2D", "2F")]
public static partial class Vector2FExtension
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[return: T]
	public static ref Overloader.Examples.Vector2<float> Sum([Integrity][T] this ref Overloader.Examples.Vector2<float> vec1, [T] float vec2X, float vec2Y)
	{
		vec1.X += vec2X;
		vec1.Y += vec2Y;
		return ref vec1;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[return: T]
	public static ref Overloader.Examples.Vector2<float> Sum([Integrity][T] this ref Overloader.Examples.Vector2<float> vec1, [T] in Overloader.Examples.Vector2<float> vec2)
	{
		vec1.X += vec2.X;
		vec1.Y += vec2.Y;
		return ref vec1;
	}
}
```

# License

Overloader is licensed under the [MIT](./LICENSE) license.
