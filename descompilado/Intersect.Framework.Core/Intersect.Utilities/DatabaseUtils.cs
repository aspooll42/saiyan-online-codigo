using System.Collections.Generic;
using Newtonsoft.Json;

namespace Intersect.Utilities;

public static class DatabaseUtils
{
	public static int[] LoadIntArray(string json, int arrayLen)
	{
		int[] array = new int[arrayLen];
		List<int> list = new List<int>();
		if (json != null)
		{
			list = JsonConvert.DeserializeObject<List<int>>(json);
		}
		for (int i = 0; i < arrayLen && i < list.Count; i++)
		{
			array[i] = list[i];
		}
		return array;
	}

	public static void LoadIntArray(ref int[] output, string json, int arrayLen)
	{
		List<int> list = JsonConvert.DeserializeObject<List<int>>(json);
		for (int i = 0; i < arrayLen && i < list.Count; i++)
		{
			output[i] = list[i];
		}
	}

	public static string SaveIntArray(int[] array, int arrayLen)
	{
		if (array == null)
		{
			array = new int[arrayLen];
		}
		List<int> list = new List<int>();
		for (int i = 0; i < arrayLen; i++)
		{
			if (i < array.Length)
			{
				list.Add(array[i]);
			}
			else
			{
				list.Add(0);
			}
		}
		return JsonConvert.SerializeObject(list);
	}

	public static long[] LoadLongArray(string json, long arrayLen)
	{
		long[] array = new long[arrayLen];
		List<long> list = ((json != null) ? JsonConvert.DeserializeObject<List<long>>(json) : new List<long>());
		for (int i = 0; i < arrayLen && i < list.Count; i++)
		{
			array[i] = list[i];
		}
		return array;
	}

	public static void LoadLongArray(ref long[] output, string json, long arrayLen)
	{
		List<long> list = JsonConvert.DeserializeObject<List<long>>(json);
		for (int i = 0; i < arrayLen && i < list.Count; i++)
		{
			output[i] = list[i];
		}
	}

	public static string SaveLongArray(long[] array, long arrayLen)
	{
		if (array == null)
		{
			array = new long[arrayLen];
		}
		List<long> list = new List<long>();
		for (int i = 0; i < arrayLen; i++)
		{
			list.Add((i < array.Length) ? array[i] : 0);
		}
		return JsonConvert.SerializeObject(list);
	}

	public static string SaveColor(Color color)
	{
		if (color == null)
		{
			color = new Color();
		}
		return JsonConvert.SerializeObject(color);
	}

	public static Color LoadColor(string json)
	{
		Color result = new Color();
		if (json != null)
		{
			result = JsonConvert.DeserializeObject<Color>(json);
		}
		return result;
	}
}
