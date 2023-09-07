// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace Overloader.Compatibility;

/// <summary>
///     Default implementation of <see href="https://github.com/dotnet/aspnetcore/blob/main/src/ObjectPool/src/DefaultObjectPool.cs" />.
/// </summary>
/// <typeparam name="T">The type to pool objects for.</typeparam>
/// <remarks>
///     This implementation keeps a cache of retained objects. This means that if objects are returned when the pool has already reached "maximumRetained"
///     objects they will be available to be Garbage Collected.
/// </remarks>
internal sealed class ObjectPool<T> where T : class?
{
	private readonly Func<T> _createFunc;

	private readonly ConcurrentQueue<T> _items = new();
	private readonly int _maxCapacity;
	private T? _fastItem;
	private int _numItems;

	public ObjectPool(Func<T> createFunc, int maxCapacity)
	{
		_createFunc = createFunc;
		_maxCapacity = maxCapacity;
	}

	public T Get()
	{
		var item = _fastItem;
		if (item != null && Interlocked.CompareExchange(ref _fastItem, null, item) == item)
			return item;

		if (!_items.TryDequeue(out item))
			return _createFunc();

		Interlocked.Decrement(ref _numItems);
		return item;
	}

	public void Return(T obj)
	{
		if (_fastItem == null && Interlocked.CompareExchange(ref _fastItem, obj, null) == null)
			return;

		if (Interlocked.Increment(ref _numItems) <= _maxCapacity)
		{
			_items.Enqueue(obj);
			return;
		}

		Interlocked.Decrement(ref _numItems);
	}
}
