using System.Text;
using Microsoft.CodeAnalysis;

namespace Overloader.Entities;

internal partial record GeneratorSourceBuilder
{
	private const string PaddingStr = "\t";
	private const string NestedUpStr = "{";
	private const string NestedDownStr = "}";
	private const string DefaultHeader = @"";

	private readonly StringBuilder _data = new(DefaultHeader);
	private uint _nestedLevel;

	public GeneratorSourceBuilder AppendUsings(IEnumerable<SyntaxNode> usings)
	{
		foreach (var @using in usings)
			Append(@using.ToFullString(), 1);

		return Append(string.Empty, 1);
	}

	public GeneratorSourceBuilder Append(string? str, sbyte breakCount = 0, char breakChar = '\n')
	{
		AppendNestingByTabs();
		_data.Append(str?.Trim() ?? string.Empty);
		for (int index = 0; index < breakCount; index++)
			_data.Append(breakChar);

		return this;
	}

	public GeneratorSourceBuilder AppendLineAndNestedIncrease(string str)
	{
		Append(str, 1);
		NestedIncrease();

		return this;
	}

	public GeneratorSourceBuilder AppendLineAndNestedDecrease(string? str = null)
	{
		Append(str, 1);
		NestedDecrease();

		return this;
	}

	public GeneratorSourceBuilder NestedIncrease()
	{
		Append(NestedUpStr, 1);
		_nestedLevel++;

		return this;
	}

	public GeneratorSourceBuilder NestedDecrease()
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

		string result = _data.ToString();
		_nestedLevel = 0;
		_data.Clear();
		_data.Append(DefaultHeader);

		return result;
	}

	public override string ToString() => _data.ToString();

	private void AppendNestingByTabs()
	{
		for (int index = 0; index < _nestedLevel; index++)
			_data.Append(PaddingStr);
	}
}
