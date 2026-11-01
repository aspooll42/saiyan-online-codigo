using System;
using System.Collections.Generic;
using System.Linq;

namespace Intersect.Utilities;

public static class ValueUtils
{
	public static void Swap<T>(ref T a, ref T b)
	{
		T val = a;
		a = b;
		b = val;
	}

	public static bool SetDefault<T>(bool condition, out T value)
	{
		value = default(T);
		return condition;
	}

	public static int ComputeHashCode(params object[] values)
	{
		return ComputeHashCode((IEnumerable<object>)values);
	}

	public static int ComputeHashCode<TValue>(IEnumerable<TValue> values)
	{
		return values?.Aggregate(0, (int current, TValue value) => current * -1521134295 + (value?.GetHashCode() ?? 0)) ?? 0;
	}

	public static int Compare(IEnumerable<string> a, IEnumerable<string> b, StringComparison stringComparison = StringComparison.CurrentCulture)
	{
		if (a == null)
		{
			if (b != null)
			{
				return -1;
			}
			return 0;
		}
		if (b == null)
		{
			return 1;
		}
		using IEnumerator<string> enumerator = a.GetEnumerator();
		using IEnumerator<string> enumerator2 = b.GetEnumerator();
		int num;
		do
		{
			bool flag = enumerator.MoveNext();
			bool flag2 = enumerator2.MoveNext();
			if (!flag)
			{
				return flag2 ? (-1) : 0;
			}
			if (!flag2)
			{
				return 1;
			}
			num = string.Compare(enumerator.Current, enumerator2.Current, stringComparison);
		}
		while (num == 0);
		return num;
	}
}
