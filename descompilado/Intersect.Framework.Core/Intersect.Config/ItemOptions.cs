using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace Intersect.Config;

public class ItemOptions
{
	[JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
	public List<string> RarityTiers { get; set; }

	[JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
	public Dictionary<int, List<double>> RarityRollBands { get; set; }

	public bool TryGetRarityName(int rarityLevel, [NotNullWhen(true)] out string? rarityName)
	{
		rarityName = RarityTiers.Skip(rarityLevel).FirstOrDefault();
		return rarityName != null;
	}

	public ItemOptions()
	{
		int num = 6;
		List<string> list = new List<string>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<string> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = "None";
		num2++;
		span[num2] = "Common";
		num2++;
		span[num2] = "Uncommon";
		num2++;
		span[num2] = "Rare";
		num2++;
		span[num2] = "Epic";
		num2++;
		span[num2] = "Legendary";
		num2++;
		RarityTiers = list;
		Dictionary<int, List<double>> dictionary = new Dictionary<int, List<double>>();
		num2 = 1;
		List<double> list2 = new List<double>(num2);
		CollectionsMarshal.SetCount(list2, num2);
		Span<double> span2 = CollectionsMarshal.AsSpan(list2);
		num = 0;
		span2[num] = 60.0;
		num++;
		dictionary[2] = list2;
		num = 2;
		List<double> list3 = new List<double>(num);
		CollectionsMarshal.SetCount(list3, num);
		span2 = CollectionsMarshal.AsSpan(list3);
		num2 = 0;
		span2[num2] = 40.0;
		num2++;
		span2[num2] = 75.0;
		num2++;
		dictionary[3] = list3;
		num2 = 3;
		List<double> list4 = new List<double>(num2);
		CollectionsMarshal.SetCount(list4, num2);
		span2 = CollectionsMarshal.AsSpan(list4);
		num = 0;
		span2[num] = 40.0;
		num++;
		span2[num] = 75.0;
		num++;
		span2[num] = 95.0;
		num++;
		dictionary[4] = list4;
		RarityRollBands = dictionary;
	}
}
