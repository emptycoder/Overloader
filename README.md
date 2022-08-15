<h1 align="center">Overloader</h1>

Overloader is the open-source source generator that provide unsafe generics.
The main target of this project helps to write generics who don't have any common based interface or type.
Also, create method parameters overloads to improve developer experience without any line of code.

# Features
## Specific types on generics
### Uncompilable code
```csharp
public static class GenericMath
{
	public static T Square<T>(T val) where T : double, float => val * val;
}
```
Unfortunately, in native csharp we can't set specific values to the template.
### Solution
```csharp
[Overload(typeof(float), "D", "F")]
public static class GenericMathD
{
	public static double Square([T] double val) => val * val;
}
```
P.S. GenericMath provided in preview versions of .net try to resolve this problem, but we can't restrict needed types.
## Parameter overload creation to avoid additional struct/class allocation
### User template
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
	public static Vector2<double> Sum([T] in Vector2<double> vec, [T] double val)
	{
		vec.X += val;
		vec.Y += val;

		return vec;
	}
}
```
### Generated part
```csharp
public static partial class Vector2FExtension
{
	public static void Sum(float vecX, float vecY, float val)
	{
		
	}
	
	public static void Sum(Overloader.Examples.Vector2<float> vec, float val)
	{
		var test = Convert.ToSingle(number);
		byte dd = (byte) test;
		Console.WriteLine($"TEST12442 {vector.X}");
	}
}
```

# License
Overloader is licensed under the [MIT](./LICENSE) license.
