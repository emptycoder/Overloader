﻿using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Overloader.Entities;

internal partial record GeneratorSourceBuilder
{
	private const string PaddingStr = "\t";
	private const string NestedUpStr = "{";
	private const string NestedDownStr = "}";
	private const string DefaultHeader = @"";

	private readonly StringBuilder _data = new(DefaultHeader);
	private uint _nestedLevel;
	private bool _nextLine = true;

	public GeneratorSourceBuilder AppendUsings(SyntaxNode syntax)
	{
		foreach (var @using in syntax.DescendantNodes().Where(node => node is UsingDirectiveSyntax))
			Append(@using.ToFullString(), 1);

		return Append(string.Empty, 1);
	}

	public GeneratorSourceBuilder Append(string? str, sbyte breakCount = 0) => AppendWoTrim(str?.Trim(), breakCount);

	public GeneratorSourceBuilder AppendWoTrim(string? str, sbyte breakCount = 0)
	{
		if (_nextLine)
		{
			for (int index = 0; index < _nestedLevel; index++)
				_data.Append(PaddingStr);
			_nextLine = false;
		}

		_data.Append(str ?? string.Empty);
		if (breakCount <= 0) return this;

		for (int index = 0; index < breakCount; index++)
			_data.Append('\n');
		_nextLine = true;

		return this;
	}

	public GeneratorSourceBuilder AppendWith(string? str, string separator) => Append(str).AppendWoTrim(separator);

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
}
