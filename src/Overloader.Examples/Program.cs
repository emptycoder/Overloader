// See https://aka.ms/new-console-template for more information

using Overloader;
using Overloader.Examples;

[assembly: Formatter(typeof(Vector2<>),
	new object[] {"T"},
	new object[]
	{
		"X", "T",
		"Y", typeof(double),
		"Z", new[]
		{
			typeof(float), typeof(double),
			typeof(double), typeof(long)
		}
	})]

Vector2FExtension.Sum(123, 1, 2, 3);
Console.WriteLine("TEST");

namespace Overloader.Examples
{
	public struct Vector2<T>
	{
		public double X;
		public double Y { get; set; }
	}

	[NewClassOverload("2D", "2F", typeof(float))]
	public static partial class Vector2DExtension
	{
		public static void Sum([T] double number, [T] Vector2<double> vector)
		{
			//$ var test = Convert.ToSingle(number); : float
			long test = Convert.ToInt64(number);
			//# "(byte)" -> "(${T})"
			byte dd = (byte) test;
			Console.WriteLine($"TEST12442 {vector.X}");
		}
	}
}
