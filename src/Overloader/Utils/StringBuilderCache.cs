using System.Text;

namespace Overloader.Utils;

internal static class StringBuilderCache
{
	private const int CountOfStringBuilders = 4;
	private static readonly StringBuilder?[] StringBuilderNodes = new StringBuilder?[CountOfStringBuilders];
	private static readonly Dictionary<int, int> HashCodes = new(CountOfStringBuilders);

	private static TaskCompletionSource<int>? _taskCompletion;

	static StringBuilderCache()
	{
		for (int sbIndex = 0; sbIndex < CountOfStringBuilders; sbIndex++)
		{
			var sb = new StringBuilder(100);
			StringBuilderNodes[sbIndex] = sb;
			HashCodes.Add(sb.GetHashCode(), sbIndex);
		}
	}

	public static async ValueTask<StringBuilder> Acquire(string? value = null)
	{
		Monitor.Enter(StringBuilderNodes);
		try
		{
			for (int sbIndex = 0; sbIndex < CountOfStringBuilders; sbIndex++)
			{
				if (StringBuilderNodes[sbIndex] is null) continue;

				return StringBuilderNodes[sbIndex]!.Append(value ?? string.Empty);
			}

			_taskCompletion = new TaskCompletionSource<int>();
			int completedSbIndex = await _taskCompletion.Task.ConfigureAwait(false);
			return StringBuilderNodes[completedSbIndex]!.Append(value ?? string.Empty);
		}
		finally
		{
			Monitor.Exit(StringBuilderNodes);
		}
	}

	public static string GetAndReturn(this StringBuilder sb)
	{
		if (!HashCodes.TryGetValue(sb.GetHashCode(), out int sbIndex))
			throw new ArgumentException($"{nameof(StringBuilder)} don't belongs to {nameof(StringBuilderCache)}.");

		var data = sb.ToString();
		StringBuilderNodes[sbIndex] = sb.Clear();
		_taskCompletion?.SetResult(sbIndex);

		return data;
	}
}
