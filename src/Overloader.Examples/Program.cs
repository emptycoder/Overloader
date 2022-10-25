// See https://aka.ms/new-console-template for more information

using System.Runtime.Intrinsics;
using Overloader;
using Overloader.Examples;

// [assembly: Formatter(typeof(Vector3<>),
// 	new object[] {"T"},
// 	new object[]
// 	{
// 		nameof(Vector3<double>.X), "T",
// 		nameof(Vector3<double>.Y), "T",
// 		nameof(Vector3<double>.Z), "T"
// 	},
// 	new object[]
// 	{
// 		typeof(Vector2<>),
// 		new object[]
// 		{
// 			nameof(Vector3<double>.X), nameof(Vector2<double>.X),
// 			nameof(Vector3<double>.Y), nameof(Vector2<double>.Y)
// 		}
// 	},
// 	new object[]
// 	{
// 		typeof(Vector2Test<>),
// 		new object[]
// 		{
// 			nameof(Vector3<double>.X), nameof(Vector2Test<double>.X),
// 			nameof(Vector3<double>.Y), nameof(Vector2Test<double>.Y)
// 		}
// 	})]
// [assembly: Formatter(typeof(Vector2<>),
// 	new object[] {"T"},
// 	new object[]
// 	{
// 		nameof(Vector3<double>.X), "T",
// 		nameof(Vector3<double>.Y), "T"
// 	})]
// [assembly: Formatter(typeof(Vector2Test<>),
// 	new object[] {"T"},
// 	new object[]
// 	{
// 		nameof(Vector3<double>.X), "T",
// 		nameof(Vector3<double>.Y), "T"
// 	})]
[assembly: Formatter(typeof(Vector2<>),
	new object[] {"T"},
	new object[]
	{
		nameof(Vector2<double>.X), "T",
		nameof(Vector2<double>.Y), "T"
	})]

[assembly: Formatter(typeof(Vector128<>),
	new object[] {"T"},
	new object[]
	{
		0, "T",
		1, "T"
	},
	// ReSharper disable once RedundantExplicitParamsArrayCreation
	new object[]
	{
		typeof(Vector2<>),
		"System.Runtime.CompilerServices.Unsafe.As<${T}, Vector128<${T}>>(ref ${Var}.X)"
	}
	// new object[]
	// {
	// 	typeof(Vector2<>),
	// 	new object[]
	// 	{
	// 		// 0, nameof(Vector2<double>.X),
	// 		// 1, nameof(Vector2<double>.Y)
	// 	}
	// }
)]

// var vec3 = new Vector3<float>();
// vec3.Sum();
var vec = Vector128.Create(123d, 123d);
vec.Sum(vec);
Console.WriteLine("TEST123");

namespace Overloader.Examples
{
	public record struct Vector3<T>
	{
		public T X;
		public T Y;
		public T Z;
	}

	public record struct Vector2<T>
	{
		public T X;
		public T Y;
	}

	public record struct Vector2Test<T>
	{
		public T X;
		public T Y;
	}

	[TSpecify(typeof(double))]
	// [Overload(typeof(float), "2D", "2F")]
	public static partial class Vector2DExtension
	{
		// [MethodImpl(MethodImplOptions.AggressiveInlining)]
		// [return: T]
		// public static ref Vector3<double> Sum(
		// 	[Integrity] [T] this ref Vector3<double> vec1,
		// 	[T][CombineWith(nameof(vec1))] in Vector3<double> vec2)
		// {
		// 	vec1.X += vec2.X;
		// 	vec1.Y += vec2.Y;
		// 	Console.WriteLine("dd1232");
		//
		// 	return ref vec1;
		// }

		[return: T]
		public static Vector128<double> Sum(
			[T] [Integrity] this Vector128<double> curr,
			[T] [Integrity] [CombineWith(nameof(curr))] Vector128<double> vector) =>
			Vector128.Create(curr[1] + vector[0], curr[1] + vector[1]);
	}
}
