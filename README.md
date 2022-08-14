<h1 align="center">Overloader</h1>

Overloader is the open-source source generator that provide unsafe generics.
The main target of this project helps to write generics who don't have any common based interface or type.
Also, create method parameters overloads to improve developer experience without any line of code.

# Problems that were resolved
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
[NewClassOverload("D", "F", typeof(float))]
public static class GenericMathD
{
	public static double Square([T] double val) => val * val;
}
```
P.S. GenericMath provided in preview versions of .net try to resolve this problem, but we can't restrict needed types.
## 

# License
Overloader is licensed under the [MIT](./LICENSE) license.
