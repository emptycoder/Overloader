using Microsoft.CodeAnalysis.Text;

namespace Overloader.Utils;

internal static class SourceTextExtensions
{
	public static string GetInnerText(this SourceText sourceText)
	{
		int startPos = sourceText.IndexOf('"') + 1;
		int endPos = sourceText.LastIndexOf('"');

		if (startPos >= endPos) throw new ArgumentException($"Can't get inner text for {sourceText}");

		return sourceText.GetSubText(new TextSpan(startPos, endPos - startPos)).ToString();
	}

	public static int IndexOf(this SourceText sourceText, char character, int startPos = 0)
	{
		for (int index = startPos; index < sourceText.Length; index++)
		{
			if (sourceText[index] != character) continue;
			return index;
		}

		return -1;
	}

	public static int LastIndexOf(this SourceText sourceText, char character)
	{
		for (int index = sourceText.Length - 1; index >= 0; index--)
		{
			if (sourceText[index] != character) continue;
			return index;
		}

		return -1;
	}
}
