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
vec2.Sum(Array.Empty<Vector2<float>>());
Console.WriteLine("TEST");

namespace Overloader.Examples
{
	public struct Vector2<T>
	{
		public T X;
		public T Y;
	}

	[Overload(typeof(float), "2D", "2F")]
	public static class Vector2DExtension
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: T]
		public static ref Vector2<double> Sum([Integrity] [T] this ref Vector2<double> vec1, [T] in Vector2<double>[] vec2)
		{
			vec1.X += vec2[0].X;
			vec1.Y += vec2[0].Y;
			Console.WriteLine("dd");

			return ref vec1;
		}
	}
}
