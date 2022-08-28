using System.Runtime.InteropServices;
using Overloader.Entities;

namespace Overloader.Utils;

internal static class StringExtensions
{
	// ReSharper disable once InconsistentNaming
	public static (string Key, string Value) SplitAsKV(this ReadOnlySpan<char> data, string separator)
	{
		int separatorIndex = data.IndexOf(separator.AsSpan(), StringComparison.Ordinal);
		if (separatorIndex == -1) throw new ArgumentException($"Separator '{separator}' not found for {data.ToString()}.");

		var key = data.Slice(0, separatorIndex).ChangeBoundsByChar();
		var value = data.Slice(separatorIndex + separator.Length).ChangeBoundsByChar();

		return (key.ToString(), value.ToString());
	}

	private static ReadOnlySpan<char> ChangeBoundsByChar(this ReadOnlySpan<char> value, char boundChar = '"')
	{
		int start = value.IndexOf(boundChar);
		if (start == -1) throw new ArgumentException($"First '\"' not found for {value.ToString()}.");

		int end = value.LastIndexOf(boundChar);
		if (end <= start + 1) throw new ArgumentException($"Last '\"' not found for {value.ToString()}.");

		return value.Slice(start + 1, end - start - 1);
	}
}
