using System;
using System.Collections;
using System.Collections.Generic;

namespace Intersect;

public class AlphanumComparatorFast : IComparer, IComparer<string>
{
	public int Compare(object x, object y)
	{
		return Compare(x as string, y as string);
	}

	public int Compare(string x, string y)
	{
		if (string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y))
		{
			return 0;
		}
		int length = x.Length;
		int length2 = y.Length;
		int num = 0;
		int num2 = 0;
		while (num < length && num2 < length2)
		{
			char c = x[num];
			char c2 = y[num2];
			char[] array = new char[length];
			int num3 = 0;
			char[] array2 = new char[length2];
			int num4 = 0;
			do
			{
				array[num3++] = c;
				num++;
				if (num >= length)
				{
					break;
				}
				c = x[num];
			}
			while (char.IsDigit(c) == char.IsDigit(array[0]));
			do
			{
				array2[num4++] = c2;
				num2++;
				if (num2 >= length2)
				{
					break;
				}
				c2 = y[num2];
			}
			while (char.IsDigit(c2) == char.IsDigit(array2[0]));
			string text = new string(array);
			string text2 = new string(array2);
			int num5;
			if (char.IsDigit(array[0]) && char.IsDigit(array2[0]))
			{
				int result = 0;
				int result2 = 0;
				int.TryParse(text, out result);
				int.TryParse(text2, out result2);
				num5 = result.CompareTo(result2);
			}
			else
			{
				num5 = string.Compare(text, text2, StringComparison.Ordinal);
			}
			if (num5 != 0)
			{
				return num5;
			}
		}
		return length - length2;
	}
}
