// See https://aka.ms/new-console-template for more information

using System.Runtime.CompilerServices;
using Overloader;
using Overloader.Examples;

[assembly: Formatter(typeof(Vector3<>),
	new object[] {"T"},
	new object[]
	{
		nameof(Vector3<double>.X), "T",
		nameof(Vector3<double>.Y), "T",
		nameof(Vector3<double>.Z), "T"
	},
	new object[]
	{
		typeof(Vector2<>),
		new object[]
		{
			nameof(Vector3<double>.X), nameof(Vector2<double>.X),
			nameof(Vector3<double>.Y), nameof(Vector2<double>.Y)
		}
	},
	new object[]
	{
		typeof(Vector2Test<>),
		new object[]
		{
			nameof(Vector3<double>.X), nameof(Vector2Test<double>.X),
			nameof(Vector3<double>.Y), nameof(Vector2Test<double>.Y)
		}
	})]
[assembly: Formatter(typeof(Vector2<>),
	new object[] {"T"},
	new object[]
	{
		nameof(Vector3<double>.X), "T",
		nameof(Vector3<double>.Y), "T"
	})]
[assembly: Formatter(typeof(Vector2Test<>),
	new object[] {"T"},
	new object[]
	{
		nameof(Vector3<double>.X), "T",
		nameof(Vector3<double>.Y), "T"
	})]

var vec3 = new Vector3<float>();
vec3.Sum();
Console.WriteLine("TEST");

namespace Overloader.Examples
{
	public struct Vector3<T>
	{
		public T X;
		public T Y;
		public T Z;
	}
	
	public struct Vector2<T>
	{
		public T X;
		public T Y;
	}
	
	public struct Vector2Test<T>
	{
		public T X;
		public T Y;
	}

	[TSpecify(typeof(double))]
	[Overload(typeof(float), "2D", "2F")]
	public static partial class Vector2DExtension
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: T]
		public static ref Vector3<double> Sum([Integrity] [T] this ref Vector3<double> vec1,
			[T][CombineWith(nameof(vec1))] in Vector3<double> vec2)
		{
			vec1.X += vec2.X;
			vec1.Y += vec2.Y;
			Console.WriteLine("dd1232");

			return ref vec1;
		}
	}
}
