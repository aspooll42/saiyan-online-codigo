using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Intersect.Extensions;

public static class EnumerableExtensions
{
	public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> enumerable, Func<KeyValuePair<TKey, TValue>, bool> where = null)
	{
		IEnumerable<KeyValuePair<TKey, TValue>> source = enumerable;
		if (where != null)
		{
			source = source.Where(where);
		}
		return source.ToDictionary((KeyValuePair<TKey, TValue> pair) => pair.Key, (KeyValuePair<TKey, TValue> pair) => pair.Value);
	}

	public static IEnumerable<T> SelectManyRecursive<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> selector, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken == default(CancellationToken) || cancellationToken.IsCancellationRequested)
		{
			return Enumerable.Empty<T>();
		}
		IEnumerable<T> enumerable = source.SelectMany(selector);
		if (cancellationToken.IsCancellationRequested)
		{
			return Enumerable.Empty<T>();
		}
		if (!enumerable.Any())
		{
			return enumerable;
		}
		return enumerable.Concat(enumerable.SelectManyRecursive(selector, cancellationToken));
	}
}
