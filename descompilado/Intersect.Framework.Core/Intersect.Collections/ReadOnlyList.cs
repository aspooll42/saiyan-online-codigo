using System.Collections;
using System.Collections.Generic;

namespace Intersect.Collections;

public class ReadOnlyList<T> : IReadOnlyList<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>
{
	private IList<T> backingList;

	public T this[int index] => backingList[index];

	public int Count => backingList.Count;

	public IEnumerator<T> GetEnumerator()
	{
		return backingList.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return backingList.GetEnumerator();
	}

	public ReadOnlyList(IList<T> backingList)
	{
		this.backingList = backingList;
	}
}
