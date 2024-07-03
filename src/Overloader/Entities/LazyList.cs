namespace Overloader.Entities;

public struct LazyList<T>
{
	private List<T>? _list;
	public List<T> Value => _list ??= new List<T>();
}
