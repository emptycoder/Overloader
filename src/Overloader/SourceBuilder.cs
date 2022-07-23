using System.Text;
using Microsoft.CodeAnalysis;

namespace Overloader;

public sealed class SourceBuilder
{
	private const string PaddingStr = "\t";
	private const string NestedUpStr = "{";
	private const string NestedDownStr = "}";
	private const string DefaultHeader = @"";
	
	private uint _nestedLevel;
	private readonly StringBuilder _sb = new(DefaultHeader);

	public SourceBuilder AppendUsings(IEnumerable<SyntaxNode> usings)
	{
		foreach (var @using in usings)
			AppendLine(@using.ToFullString());
		
		return this;
	}

	public SourceBuilder AppendLine(string? str = null)
	{
		AppendNestingByTabs();
		_sb.AppendLine(str ?? string.Empty);
		
		return this;
	}

	public SourceBuilder Append(string str)
	{
		AppendNestingByTabs();
		_sb.Append(str);
		
		return this;
	}

	public SourceBuilder AppendLineAndNestedIncrease(string str)
	{
		AppendLine(str);
		NestedIncrease();
		
		return this;
	}

	public SourceBuilder AppendLineAndNestedDecrease(string str)
	{
		AppendLine(str);
		NestedDecrease();
		
		return this;
	}

	public SourceBuilder NestedIncrease()
	{
		AppendLine(NestedUpStr);
		_nestedLevel++;

		return this;
	}

	public SourceBuilder NestedDecrease()
	{
		if (_nestedLevel == 0)
			throw new Exception("Minimum nested level has been reached");
		AppendLine(NestedDownStr);
		_nestedLevel--;
		
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
