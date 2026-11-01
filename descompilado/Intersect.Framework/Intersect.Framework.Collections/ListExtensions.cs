using System;
using System.Collections.Generic;

namespace Intersect.Framework.Collections;

public static class ListExtensions
{
	private sealed class KeyComparer<TItem, TKey>(Func<TItem?, TKey?> keySelector) : IComparer<TItem> where TKey : IComparable<TKey>
	{
		public int Compare(TItem? x, TItem? y)
		{
			TKey val = keySelector(x);
			TKey val2 = keySelector(y);
			if (val != null)
			{
				return val.CompareTo(val2);
			}
			if (val2 == null)
			{
				return 0;
			}
			return -val2.CompareTo(val);
		}
	}

	public static void AddSorted<T>(this List<T> @this, T item) where T : IComparable<T>
	{
		if (@this.Count == 0)
		{
			@this.Add(item);
			return;
		}
		if (@this[@this.Count - 1].CompareTo(item) <= 0)
		{
			@this.Add(item);
			return;
		}
		if (@this[0].CompareTo(item) >= 0)
		{
			@this.Insert(0, item);
			return;
		}
		int num = @this.BinarySearch(item);
		if (num < 0)
		{
			num = ~num;
		}
		@this.Insert(num, item);
	}

	public static void Resort<T>(this List<T> @this, T item) where T : IComparable<T>
	{
		if (@this.Count < 1)
		{
			@this.AddSorted(item);
			return;
		}
		@this.Remove(item);
		@this.AddSorted(item);
	}

	public static void AddSorted<TItem, TKey>(this List<TItem> @this, TItem item, Func<TItem?, TKey?> keySelector) where TKey : IComparable<TKey>
	{
		if (@this.Count == 0)
		{
			@this.Add(item);
			return;
		}
		KeyComparer<TItem, TKey> keyComparer = new KeyComparer<TItem, TKey>(keySelector);
		if (keyComparer.Compare(@this[@this.Count - 1], item) <= 0)
		{
			@this.Add(item);
			return;
		}
		if (keyComparer.Compare(@this[0], item) >= 0)
		{
			@this.Insert(0, item);
			return;
		}
		int num = @this.BinarySearch(item, keyComparer);
		if (num < 0)
		{
			num = ~num;
		}
		@this.Insert(num, item);
	}

	public static void Resort<TItem, TKey>(this List<TItem> @this, TItem item, Func<TItem?, TKey?> keySelector) where TKey : IComparable<TKey>
	{
		if (@this.Count < 1)
		{
			@this.AddSorted(item, keySelector);
			return;
		}
		@this.Remove(item);
		@this.AddSorted(item, keySelector);
	}
}
