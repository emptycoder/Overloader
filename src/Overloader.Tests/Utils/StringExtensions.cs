namespace Overloader.Tests.Utils;

public static class StringExtensions
{
	public static string Minify(this string str) =>
		str.Replace("\t", "")
			.Replace("\n", "")
			.Replace("\r", "");
}
