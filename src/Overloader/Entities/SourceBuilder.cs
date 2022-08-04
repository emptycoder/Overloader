using System.Text;
using Microsoft.CodeAnalysis;

namespace Overloader.Entities;

public class SourceBuilder
{
	private const string PaddingStr = "\t";
	private const string NestedUpStr = "{";
	private const string NestedDownStr = "}";
	private const string DefaultHeader = @"";
	private readonly StringBuilder _sb = new(DefaultHeader);

	private uint _nestedLevel;

	public SourceBuilder AppendUsings(IEnumerable<SyntaxNode> usings)
	{
		foreach (var @using in usings)
			Append(@using.ToFullString(), 1);

		return this;
	}

	public SourceBuilder Append(string? str, sbyte breakCount = 0, char breakChar = '\n')
	{
		AppendNestingByTabs();
		_sb.Append(str?.Trim() ?? string.Empty);
		for (int index = 0; index < breakCount; index++)
			_sb.Append(breakChar);

		return this;
	}

	public SourceBuilder AppendLineAndNestedIncrease(string str)
	{
		Append(str, 1);
		NestedIncrease();

		return this;
	}

	public SourceBuilder AppendLineAndNestedDecrease(string? str = null)
	{
		Append(str, 1);
		NestedDecrease();

		return this;
	}

	public SourceBuilder NestedIncrease()
	{
		Append(NestedUpStr, 1);
		_nestedLevel++;

		return this;
	}

	public SourceBuilder NestedDecrease()
	{
		if (_nestedLevel == 0)
			throw new Exception("Minimum nested level has been reached");
		_nestedLevel--;
		Append(NestedDownStr, 1);

		return this;
	}

	public string ToStringAndClear()
	{
		if (_nestedLevel != 0) throw new Exception($"Nesting must be completed (Increase/Decrease): {_nestedLevel}");

		string result = _sb.ToString();
		_nestedLevel = 0;
		_sb.Clear();
		_sb.Append(DefaultHeader);

		return result;
	}

	private void AppendNestingByTabs()
	{
		for (int index = 0; index < _nestedLevel; index++)
			_sb.Append(PaddingStr);
	}
}
