using System;
using System.Collections;
using System.Text;

namespace Intersect.Utilities;

public class AlphanumComparator : IComparer
{
	private enum ChunkType
	{
		Alphanumeric,
		Numeric
	}

	public int Compare(object x, object y)
	{
		string text = x as string;
		string text2 = y as string;
		if (text == null || text2 == null)
		{
			return 0;
		}
		int num = 0;
		int num2 = 0;
		while (num < text.Length || num2 < text2.Length)
		{
			if (num >= text.Length)
			{
				return -1;
			}
			if (num2 >= text2.Length)
			{
				return 1;
			}
			char c = text[num];
			char c2 = text2[num2];
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder2 = new StringBuilder();
			while (num < text.Length && (stringBuilder.Length == 0 || InChunk(c, stringBuilder[0])))
			{
				stringBuilder.Append(c);
				num++;
				if (num < text.Length)
				{
					c = text[num];
				}
			}
			while (num2 < text2.Length && (stringBuilder2.Length == 0 || InChunk(c2, stringBuilder2[0])))
			{
				stringBuilder2.Append(c2);
				num2++;
				if (num2 < text2.Length)
				{
					c2 = text2[num2];
				}
			}
			int num3 = 0;
			if (char.IsDigit(stringBuilder[0]) && char.IsDigit(stringBuilder2[0]))
			{
				int num4 = Convert.ToInt32(stringBuilder.ToString());
				int num5 = Convert.ToInt32(stringBuilder2.ToString());
				if (num4 < num5)
				{
					num3 = -1;
				}
				if (num4 > num5)
				{
					num3 = 1;
				}
			}
			else
			{
				num3 = string.Compare(stringBuilder.ToString(), stringBuilder2.ToString(), StringComparison.Ordinal);
			}
			if (num3 != 0)
			{
				return num3;
			}
		}
		return 0;
	}

	private static bool InChunk(char ch, char otherCh)
	{
		ChunkType chunkType = ChunkType.Alphanumeric;
		if (char.IsDigit(otherCh))
		{
			chunkType = ChunkType.Numeric;
		}
		if (chunkType != ChunkType.Alphanumeric || !char.IsDigit(ch))
		{
			if (chunkType == ChunkType.Numeric)
			{
				return char.IsDigit(ch);
			}
			return true;
		}
		return false;
	}
}
