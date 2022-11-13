namespace Overloader.Utils;

internal static class StringExtensions
{
	public static bool TryToFindMatch(this ReadOnlySpan<char> data, ReadOnlySpan<char> entry, string separator)
	{
		for (;;)
		{
			int matchIndex = data.IndexOf(separator.AsSpan(), StringComparison.Ordinal);
			if (matchIndex == -1) return data.Trim().SequenceEqual(entry);
			if (data.Slice(0, matchIndex).Trim().SequenceEqual(entry)) return true;
			if (matchIndex + 1 >= data.Length) return false;
			data = data.Slice(matchIndex + 1);
		}
	}

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
