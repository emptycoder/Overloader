// See https://aka.ms/new-console-template for more information

using Overloader;

var test = new TestProject.Vector2FExtension();
Console.WriteLine("TEST");

namespace TestProject
{
	[NewClassOverload("2D", "2F", typeof(float))]
	public partial class Vector2DExtension
	{
		[return: T]
		public void Sum([T] double number)
		{
			//$ var test = Convert.ToSingle(number); : Single
			var test = Convert.ToInt64(number);
			//# "(byte)" -> "({typeSyntax})"
			byte dd = (byte) test;
		}
	}
}
