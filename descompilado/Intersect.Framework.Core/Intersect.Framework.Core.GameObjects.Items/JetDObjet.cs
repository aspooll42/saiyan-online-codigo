using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Enums;
using Intersect.GameObjects.Ranges;

namespace Intersect.Framework.Core.GameObjects.Items;

public static class JetDObjet
{
	public const int PlafondMaximum = 4;

	public static IEnumerable<(Stat Stat, int Bas, int Haut)> Fourchettes(ItemDescriptor? descriptor)
	{
		Dictionary<Stat, ItemRange> dictionary = descriptor?.EquipmentProperties?.StatRanges;
		if (dictionary == null)
		{
			yield break;
		}
		KeyValuePair<Stat, ItemRange>[] array = dictionary.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			KeyValuePair<Stat, ItemRange> keyValuePair = array[i];
			var (item, itemRange2) = keyValuePair;
			if (itemRange2 != null && itemRange2.HighRange > itemRange2.LowRange)
			{
				yield return (Stat: item, Bas: itemRange2.LowRange, Haut: itemRange2.HighRange);
			}
		}
	}

	public static bool ADesJets(ItemDescriptor? descriptor)
	{
		return Fourchettes(descriptor).Any();
	}

	public static double? Qualite(ItemDescriptor? descriptor, ItemProperties? properties)
	{
		int[] array = properties?.StatModifiers;
		if (array == null)
		{
			return null;
		}
		double num = 0.0;
		int num2 = 0;
		foreach (var item4 in Fourchettes(descriptor))
		{
			Stat item = item4.Stat;
			int item2 = item4.Bas;
			int item3 = item4.Haut;
			int num3 = (int)item;
			if (num3 >= 0 && num3 < array.Length)
			{
				num += Math.Clamp((double)(array[num3] - item2) / (double)(item3 - item2), 0.0, 1.0);
				num2++;
			}
		}
		if (num2 != 0)
		{
			return num / (double)num2;
		}
		return null;
	}

	public static bool EstParfait(double? qualite)
	{
		return qualite >= 0.9999;
	}

	public static int Plafond(ItemDescriptor? descriptor)
	{
		if (descriptor == null || descriptor.Rarity < 2 || !ADesJets(descriptor))
		{
			return 0;
		}
		return Math.Min(descriptor.Rarity, 4);
	}

	public static double[] Bornes(int plafond)
	{
		Dictionary<int, List<double>> dictionary = Options.Instance?.Items?.RarityRollBands;
		if (dictionary != null && dictionary.TryGetValue(plafond, out var value) && value != null && value.Count > 0)
		{
			return (from borne in value
				select Math.Clamp(borne / 100.0, 0.0, 1.0) into borne
				orderby borne
				select borne).ToArray();
		}
		return Array.Empty<double>();
	}

	public static int Rarete(ItemDescriptor? descriptor, ItemProperties? properties)
	{
		if (descriptor == null)
		{
			return 0;
		}
		int num = Plafond(descriptor);
		double[] array = Bornes(num);
		if (num != 0 && array.Length != 0)
		{
			double? num2 = Qualite(descriptor, properties);
			if (num2.HasValue)
			{
				double qualite = num2.GetValueOrDefault();
				return Math.Min(1 + array.Count((double borne) => qualite >= borne - 1E-09), num);
			}
		}
		return descriptor.Rarity;
	}

	public static (int Min, int Max) Intervalle(int bas, int haut, int plafond, int rarete)
	{
		double[] array = Bornes(plafond);
		int num = haut - bas;
		if (num <= 0 || array.Length == 0)
		{
			return (Min: bas, Max: Math.Max(bas, haut));
		}
		int num2 = Math.Clamp(rarete, 1, array.Length + 1) - 1;
		double num3 = ((num2 == 0) ? 0.0 : array[num2 - 1]);
		int value = bas + (int)Math.Ceiling(num3 * (double)num - 1E-09);
		int value2 = ((num2 >= array.Length) ? haut : (bas + (int)Math.Ceiling(array[num2] * (double)num - 1E-09) - 1));
		value = Math.Clamp(value, bas, haut);
		value2 = Math.Clamp(value2, bas, haut);
		return (Min: value, Max: Math.Max(value, value2));
	}
}
