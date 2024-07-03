namespace Overloader.Utils;

public static class StringExtensions
{
	public static int FindInReplacements(this string value, Span<(string VarName, string ConcatedVars)> replacements)
	{
		for (int index = 0; index < replacements.Length; index++)
		{
			if (!replacements[index].VarName.Equals(value)) continue;
			return index;
		}

		return -1;
	}

	public static bool TryToFindMatch(this ReadOnlySpan<char> data, ReadOnlySpan<char> entry, ReadOnlySpan<char> separator)
	{
		for (;;)
		{
			int matchIndex = data.IndexOf(separator, StringComparison.Ordinal);
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
		if (separatorIndex == -1)
			throw new ArgumentException($"Separator '{separator}' not found for {data.ToString()}.");

		var key = data.Slice(0, separatorIndex).ChangeBoundsByChar();
		var value = data.Slice(separatorIndex + separator.Length).ChangeBoundsByChar();

		if (key.Length == 0)
			throw new ArgumentException($"Key can't be empty for {data.ToString()}.");

		return (key.ToString(), value.ToString());
	}

	private static ReadOnlySpan<char> ChangeBoundsByChar(this ReadOnlySpan<char> value, char boundChar = '"')
	{
		int start = value.IndexOf(boundChar);
		if (start == -1) throw new ArgumentException($"First '\"' not found for {value.ToString()}.");

		int end = value.LastIndexOf(boundChar);
		if (end == start) throw new ArgumentException($"Last '\"' not found for {value.ToString()}.");

		return value.Slice(start + 1, end - start - 1);
	}
}
