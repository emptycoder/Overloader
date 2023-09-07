<h1 align="center">Overloader</h1>

Overloader is open-source generator for method overloads.
Support of xml documentation for overloads.

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

# Parameter overload creation to avoid additional struct/class allocation
## User template
```csharp
[assembly: Formatter(
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
	public static ref Vector2<float> Sum(this ref Vector2<float> vec1, float vec2X, float vec2Y)
	{
		vec1.X += vec2X;
		vec1.Y += vec2Y;
		return ref vec1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref Vector2<float> Sum(this ref Vector2<float> vec1, in Vector2<float> vec2)
	{
		vec1.X += vec2.X;
		vec1.Y += vec2.Y;
		return ref vec1;
	}
}
```

# Parameter overload creation with cast
## User template
```csharp
[assembly: Formatter(
	"Vector2S",
	typeof(Vector2<>),
	new object[] {"T"},
	new object[]
	{
		nameof(Vector2<short>.X), "T",
		nameof(Vector2<short>.Y), "T"
	},
	new object[]
	{
		typeof(Vector2<ushort>),
		"(short) ${Var0}.X, (short) ${Var0}.Y"
	})]

[assembly: Formatter(
	"Vector2US",
	typeof(Vector2<>),
	new object[] {"T"},
	new object[]
	{
		nameof(Vector2<ushort>.X), "T",
		nameof(Vector2<ushort>.Y), "T"
	},
	new object[]
	{
		typeof(Vector2<short>),
		"(ushort) ${Var0}.X, (ushort) ${Var0}.Y"
	})]

[TSpecify(typeof(short), "Vector2S")]
[TOverload(typeof(ushort), "2S", "2US", "Vector2US")]
public static partial class V2SAdd
{
	[return: T]
	public static ref Vector2<short> Add(
		[T] [Integrity] this ref Vector2<short> current,
		[T] Vector2<short> vector)
	{
		current.X += vector.X;
		current.Y += vector.Y;
		return ref current;
	}
}
```

## Generated part
```csharp
public static partial class V2SAdd
{
	// Generated by: DecompositionOverload
	public static ref Vector2<short> Add(this ref Vector2<short> current, short vectorX, short vectorY)
	{
		current.X += vectorX;
		current.Y += vectorY;
		return ref current;
	}
	// Generated by: CastTransitionOverloads
	public static ref Vector2<short> Add(this ref Vector2<short> current, Vector2<ushort> vector0) =>
		ref Add(ref current, (short) vector0.X, (short) vector0.Y);
}

public static partial class V2USAdd
{
	// Generated by: DecompositionOverload
	public static ref Vector2<ushort> Add(this ref Vector2<ushort> current, ushort vectorX, ushort vectorY)
	{
		current.X += vectorX;
		current.Y += vectorY;
		return ref current;
	}
	// Generated by: IntegrityOverload
	public static ref Vector2<ushort> Add(this ref Vector2<ushort> current, Vector2<ushort> vector)
	{
		current.X += vector.X;
		current.Y += vector.Y;
		return ref current;
	}
	// Generated by: CastTransitionOverloads
	public static ref Vector2<ushort> Add(this ref Vector2<ushort> current, Vector2<short> vector0) =>
		ref Add(ref current, (ushort) vector0.X, (ushort) vector0.Y);
}
```

# Parameter overload creation with ref attribute
## User template
```csharp
[assembly: Formatter(
	"Vector2S",
	typeof(Vector2<>),
	new object[] {"T"},
	new object[]
	{
		nameof(Vector2<short>.X), "T",
		nameof(Vector2<short>.Y), "T"
	})]

[assembly: Formatter(
	"Vector2US",
	typeof(Vector2<>),
	new object[] {"T"},
	new object[]
	{
		nameof(Vector2<ushort>.X), "T",
		nameof(Vector2<ushort>.Y), "T"
	})]

[TSpecify(typeof(short), "Vector2S")]
[TOverload(typeof(ushort), "2S", "2US", "Vector2US")]
public static partial class V2SAdd
{
	[return: T]
	public static ref Vector2<short> Add(
		[T] [Integrity] this ref Vector2<short> current,
		[T] [Ref] Vector2<short> vector)
	{
		current.X += vector.X;
		current.Y += vector.Y;
		return ref current;
	}
}
```

## Generated part
```csharp
public static partial class V2SAdd
{
	// Generated by: DecompositionOverload
	public static ref Vector2<short> Add(this ref Vector2<short> current, short vectorX, short vectorY)
	{
		current.X += vectorX;
		current.Y += vectorY;
		return ref current;
	}
	// Generated by: RefIntegrityOverloads
	public static ref Vector2<short> Add(this ref Vector2<short> current, ref Vector2<short> vector)
	{
		current.X += vector.X;
		current.Y += vector.Y;
		return ref current;
	}
}

public static partial class V2USAdd
{
	// Generated by: DecompositionOverload
	public static ref Vector2<ushort> Add(this ref Vector2<ushort> current, ushort vectorX, ushort vectorY)
	{
		current.X += vectorX;
		current.Y += vectorY;
		return ref current;
	}
	// Generated by: IntegrityOverload
	public static ref Vector2<ushort> Add(this ref Vector2<ushort> current, Vector2<ushort> vector)
	{
		current.X += vector.X;
		current.Y += vector.Y;
		return ref current;
	}
	// Generated by: RefIntegrityOverloads
	public static ref Vector2<ushort> Add(this ref Vector2<ushort> current, ref Vector2<ushort> vector)
	{
		current.X += vector.X;
		current.Y += vector.Y;
		return ref current;
	}
}
```

# Development
## How to debug?
- Use the [launchSettings.json](Properties/launchSettings.json) profile.
- Debug tests.

## How to see on generated sources?
Add the next PropertyGroup into your .csproj file which uses Overloader:
```xml
<PropertyGroup>
	<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
	<CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)\..\..\$(AssemblyName).Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```
See on "$(AssemblyName).Generated" folder.

## How can I determine which syntax nodes I should expect?
Consider installing the Roslyn syntax tree viewer plugin [Rossynt](https://plugins.jetbrains.com/plugin/16902-rossynt/).

# License
Overloader distributed under [MIT](./LICENSE) license.
