﻿using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Overloader.Utils;

namespace Overloader.Entities.Builders;

internal class SourceBuilder : IDisposable
{
	private static readonly HashSet<string> AttributesToRemove = new()
	{
		Constants.TSpecifyAttr,
		Constants.OverloadAttr,
		Constants.TAttr,
		Constants.CombineWithAttr,
		Constants.IntegrityAttr,
		Constants.IgnoreForAttr,
		Constants.BlackListModeAttr,
		Constants.AllowForAttr,
		Constants.ChangeModifierAttr
	};
	
	private const string PaddingStr = "\t";

	// ReSharper disable once RedundantSuppressNullableWarningExpression
	private static readonly ObjectPool<SourceBuilder> SPoolInstance = new(() =>
		new SourceBuilder(SPoolInstance!, new StringBuilder()), 16);

	private readonly StringBuilder _builder;
	private readonly ObjectPool<SourceBuilder> _pool;
	private uint _nestedLevel;
	private bool _nextLine = true;

	private protected SourceBuilder(ObjectPool<SourceBuilder> pool, StringBuilder builder) =>
		(_pool, _builder) = (pool, builder);

	public virtual void Dispose()
	{
		Clear();
		_pool.Free(this);
	}

	public SourceBuilder AppendChainMemberNameComment(string callerMemberName) =>
		AppendWoTrim($"// Generated by: {callerMemberName}", 1);

	public SourceBuilder AppendAttributes(in SyntaxList<AttributeListSyntax> attributeListSyntaxes, string separator)
	{
		foreach (var listOfAttrs in attributeListSyntaxes)
		{
			bool isOpened = false;
			foreach (var attr in listOfAttrs.Attributes)
			{
				if (AttributesToRemove.Contains(attr.Name.ToString())) continue;
				if (!isOpened && (isOpened = true))
				{
					var target = listOfAttrs.Target;
					Append(SyntaxKind.OpenBracketToken);
					if (target is not null)
						AppendWith(target.ToString(), " ");
				}
				else
					AppendWoTrim(", ");
				Append(attr.ToString());
			}

			if (isOpened) Append(SyntaxKind.CloseBracketToken).AppendWoTrim(separator);
		}

		return this;
	}

	public SourceBuilder Append(SyntaxKind syntaxKind, sbyte breakCount = 0) =>
		AppendWoTrim(SyntaxFacts.GetText(syntaxKind).AsSpan(), breakCount);

	public SourceBuilder Append(string str, sbyte breakCount = 0) =>
		AppendWoTrim(str.AsSpan().Trim(), breakCount);

	public SourceBuilder Append(ReadOnlySpan<char> str, sbyte breakCount = 0) =>
		AppendWoTrim(str.Trim(), breakCount);

	public SourceBuilder AppendWoTrim(string str, sbyte breakCount = 0) =>
		AppendWoTrim(str.AsSpan(), breakCount);

	public virtual SourceBuilder AppendWoTrim(ReadOnlySpan<char> str, sbyte breakCount = 0)
	{
		if (_nextLine)
		{
			for (int index = 0; index < _nestedLevel; index++)
				_builder.Append(PaddingStr);
			_nextLine = false;
		}

		bool isNextLineFound = false;
		for (int index = 0; index < str.Length; index++)
		{
			char character = str[index];
			_builder.Append(character);
			isNextLineFound |= character is '\n';
		}

		if (breakCount <= 0 && !isNextLineFound) return this;

		for (int index = 0; index < breakCount; index++)
			_builder.Append('\n');
		_nextLine = true;

		return this;
	}

	public SourceBuilder AppendWith(string str, string separator) => Append(str).AppendWoTrim(separator);

	public SourceBuilder NestedIncrease(SyntaxKind? withSyntax = null)
	{
		if (withSyntax.HasValue)
			Append(SyntaxFacts.GetText(withSyntax.Value), 1);
		_nestedLevel++;

		return this;
	}

	public SourceBuilder NestedDecrease(SyntaxKind? withSyntax = null)
	{
		if (_nestedLevel == 0)
			throw new Exception("Minimum nested level has been reached");

		_nestedLevel--;
		if (withSyntax.HasValue)
			Append(SyntaxFacts.GetText(withSyntax.Value), 1);

		return this;
	}

	public void Clear()
	{
		_nextLine = true;
		_nestedLevel = 0;
		_builder.Clear();
	}

	public override string ToString() => _builder.ToString();

	public string ToStringAndClear()
	{
		string result = _builder.ToString();
		Clear();
		return result;
	}

	public string ToStringAndDispose()
	{
		string result = _builder.ToString();
		Dispose();
		return result;
	}

	public SourceBuilder GetDependentInstance()
	{
		var instance = GetInstance();
		instance._nestedLevel = _nestedLevel;

		return instance;
	}

	public SourceBuilder GetClonedInstance()
	{
		var newInstance = GetInstance();
		newInstance._builder.Append(_builder);
		return newInstance;
	}

	public static SourceBuilder GetInstance() => SPoolInstance.Allocate() ?? throw new ArgumentException(
		"Can't allocate source builder instance.");
}
