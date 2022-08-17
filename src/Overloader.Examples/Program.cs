// See https://aka.ms/new-console-template for more information

using System.Runtime.CompilerServices;
using Overloader;
using Overloader.Examples;

[assembly: Formatter(typeof(Vector2<>),
	new object[] {"T"},
	new object[]
	{
		"X", "T",
		"Y", "T"
	})]

var vec2 = new Vector2<float>();
vec2.Sum(2);
Console.WriteLine("TEST");

namespace Overloader.Examples
{
	public struct Vector2<T>
	{
		public T X;
		public T Y;
	}

	[Overload(typeof(float), "2D", "2F")]
	public static partial class Vector2DExtension
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: T(typeof(Vector2<float>), typeof(float))]
		public static ref Vector2<double> Sum([Integrity][T] this ref Vector2<double> vec, [T] double val)
		{
			vec.X += val;
			vec.Y += val;
			Console.WriteLine("dd");

			return ref vec;
		}
	}
	
	[Overload(typeof(float), "D", "F")]
	public static partial class GenericMathD
	{
		public static double Square([T] double val) => val * val;
	}
}
