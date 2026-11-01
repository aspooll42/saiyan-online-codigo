using System;
using System.Collections.Generic;
using System.Linq;

namespace Intersect.Extensions;

public static class ArrayExtensions
{
	public static TType[] Prepend<TType>(this TType[] values, params TType[] prependedValues)
	{
		return new TType[0].Concat(prependedValues).Concat(values).ToArray();
	}

	public static IEnumerable<IEnumerable<T>> Split<T>(this T[] array, int size)
	{
		for (int i = 0; (float)i < (float)array.Length / (float)size; i++)
		{
			yield return array.Skip(i * size).Take(size);
		}
	}

	public static int IndexOf<T>(this T[] array, T value)
	{
		return Array.IndexOf(array, value);
	}
}
