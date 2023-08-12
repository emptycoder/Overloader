using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Overloader.Exceptions;

namespace Overloader.Entities;

public readonly struct ResultOrException<TResult>
{
	private readonly Exception? _exception;
	private readonly TResult _result;

	private ResultOrException(TResult result, Exception? exception)
	{
		_exception = exception;
		_result = result;
	}

	public static implicit operator ResultOrException<TResult>(Exception value) => new(default!, value);
	public static implicit operator ResultOrException<TResult>(TResult value) => new(value, default);

	public static ResultOrException<TResult> Exception(Exception exception) => exception;
	public static ResultOrException<TResult> Result(TResult result) => result;

	public Exception PickException => _exception ?? throw new ArgumentNullException(nameof(_exception), "Exception isn't exists.");
	public TResult PickResult(SyntaxNode? syntaxNode = null)
	{
		if (_exception is not null)
			throw syntaxNode is null
				? _exception
				: _exception.WithLocation(syntaxNode);

		return _result!;
	}

	public void PickResult(out TResult value, SyntaxNode? syntaxNode = null)
	{
		if (_exception is not null)
			throw syntaxNode is null
				? _exception
				: _exception.WithLocation(syntaxNode);

		value = _result!;
	}
	
	public TResult PickResult(Location? location = null)
	{
		if (_exception is not null)
			throw location is null
				? _exception
				: _exception.WithLocation(location);

		return _result!;
	}

	public void PickResult(out TResult value, Location? location = null)
	{
		if (_exception is not null)
			throw location is null
				? _exception
				: _exception.WithLocation(location);

		value = _result!;
	}

#nullable disable
	public void Deconstruct(out TResult value, out Exception exception)
	{
		if (_exception is not null)
		{
			value = default;
			exception = _exception;
		}

		value = _result;
		exception = default;
	}
#nullable enable

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj))
			return false;

		return obj is ResultOrException<TResult> o && Equals(o);
	}

	public override string ToString() =>
		_result is not null ? FormatValue(_result) : FormatValue(_exception);

	public override int GetHashCode() =>
		_result is not null ? _result.GetHashCode() : _exception!.GetHashCode();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string FormatValue<T>(T value) => $"{typeof(T).FullName}: {value?.ToString()}";
}
