namespace Overloader.Utils;

public static class StringExtensions
{
	// ReSharper disable once InconsistentNaming
	public static (string Key, string Value) SplitAsKV(this string data, string separator)
	{
		int separatorIndex = data.IndexOf(separator, StringComparison.Ordinal);
		if (separatorIndex == -1) throw new ArgumentException();

		var dataSpan = data.AsSpan();
		var key = dataSpan.Slice(0, separatorIndex).ChangeBoundsByChar();
		var value = dataSpan.Slice(separatorIndex + separator.Length).ChangeBoundsByChar();

		return (key.ToString(), value.ToString());
	}

	private static ReadOnlySpan<char> ChangeBoundsByChar(this ReadOnlySpan<char> value, char boundChar = '"')
	{
		int start = value.IndexOf(boundChar);
		if (start == -1) throw new ArgumentException();

		int end = value.LastIndexOf(boundChar);
		if (end == start) throw new ArgumentException();
		
		return value.Slice(start, end - start);
	}
}
