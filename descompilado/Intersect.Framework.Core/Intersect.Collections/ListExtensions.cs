using System.Collections.Generic;

namespace Intersect.Collections;

public static class ListExtensions
{
	public static IReadOnlyList<T> WrapReadOnly<T>(this IList<T> list)
	{
		return new ReadOnlyList<T>(list);
	}
}
