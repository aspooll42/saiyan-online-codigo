using System;
using System.Linq;

namespace Intersect.Framework;

public static class StringExtensions
{
	public static string Reverse(this string @string)
	{
		int[] array = new int[@string.Length];
		int num = 0;
		for (int i = 0; i < @string.Length; i++)
		{
			num++;
			array[i] = char.ConvertToUtf32(@string, i);
			if (char.IsSurrogatePair(@string, i))
			{
				i++;
			}
		}
		array = array[..num];
		Array.Reverse(array);
		return array.Aggregate(string.Empty, (string current, int codepoint) => current + char.ConvertFromUtf32(codepoint));
	}
}
