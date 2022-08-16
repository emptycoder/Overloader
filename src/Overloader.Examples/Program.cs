// See https://aka.ms/new-console-template for more information

using Overloader;
using Overloader.Examples;

[assembly: Formatter(typeof(Vector2<>),
	new object[] {"T"},
	new object[]
	{
		"X", "T",
		"Y", "T"
	})]

// Vector2FExtension.Sum(, 1, 2);
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
		[return: T(typeof(Vector2<float>), typeof(float))]
		public static ref Vector2<double> Sum([Integrity][T] this ref Vector2<double> vec, [T] double val)
		{
			vec.X += val;
			vec.Y += val;

			return ref vec;
		}
	}
}
