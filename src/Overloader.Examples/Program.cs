// See https://aka.ms/new-console-template for more information

using Overloader;
using TestProject;

[assembly:CustomOverload(typeof(Vector2<>), @"
<T> {
	X: T,
	Y: {
		float: double,
		double: long
	}
}")]

var test = TestProject.Vector2FExtension.Sum();
Console.WriteLine("TEST");

namespace TestProject
{
	public struct Vector2<T>
	{
		public double X;
		public double Y { get; set; }
	}

	[NewClassOverload("2D", "2F", typeof(float))]
	public static partial class Vector2DExtension
	{
		public static void Sum([T] double number)
		{
			//$ var test = Convert.ToSingle(number); : Single
			var test = Convert.ToInt64(number);
			//# "(byte)" -> "({typeSyntax})"
			byte dd = (byte) test;
		}
	}
}
