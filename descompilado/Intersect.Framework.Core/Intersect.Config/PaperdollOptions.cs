using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Intersect.Framework.Annotations;
using Newtonsoft.Json;

namespace Intersect.Config;

public class PaperdollOptions
{
	[Ignore]
	[JsonIgnore]
	public List<string>[] Directions;

	public List<string> Down { get; set; }

	public List<string> Left { get; set; }

	public List<string> Right { get; set; }

	public List<string> Up { get; set; }

	public PaperdollOptions()
	{
		int num = 6;
		List<string> list = new List<string>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<string> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = "Player";
		num2++;
		span[num2] = "Armor";
		num2++;
		span[num2] = "Helmet";
		num2++;
		span[num2] = "Weapon";
		num2++;
		span[num2] = "Shield";
		num2++;
		span[num2] = "Boots";
		num2++;
		Down = list;
		num2 = 6;
		List<string> list2 = new List<string>(num2);
		CollectionsMarshal.SetCount(list2, num2);
		span = CollectionsMarshal.AsSpan(list2);
		num = 0;
		span[num] = "Player";
		num++;
		span[num] = "Armor";
		num++;
		span[num] = "Helmet";
		num++;
		span[num] = "Weapon";
		num++;
		span[num] = "Shield";
		num++;
		span[num] = "Boots";
		num++;
		Left = list2;
		num = 6;
		List<string> list3 = new List<string>(num);
		CollectionsMarshal.SetCount(list3, num);
		span = CollectionsMarshal.AsSpan(list3);
		num2 = 0;
		span[num2] = "Player";
		num2++;
		span[num2] = "Armor";
		num2++;
		span[num2] = "Helmet";
		num2++;
		span[num2] = "Weapon";
		num2++;
		span[num2] = "Shield";
		num2++;
		span[num2] = "Boots";
		num2++;
		Right = list3;
		num2 = 6;
		List<string> list4 = new List<string>(num2);
		CollectionsMarshal.SetCount(list4, num2);
		span = CollectionsMarshal.AsSpan(list4);
		num = 0;
		span[num] = "Player";
		num++;
		span[num] = "Armor";
		num++;
		span[num] = "Helmet";
		num++;
		span[num] = "Weapon";
		num++;
		span[num] = "Shield";
		num++;
		span[num] = "Boots";
		num++;
		Up = list4;
		Directions = new List<string>[4] { Up, Down, Left, Right };
	}

	[OnDeserializing]
	internal void OnDeserializingMethod(StreamingContext context)
	{
		Up.Clear();
		Down.Clear();
		Left.Clear();
		Right.Clear();
	}

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		List<string> list = new List<string>();
		list.AddRange(Up.Distinct());
		Up = list;
		List<string> list2 = new List<string>();
		list2.AddRange(Down.Distinct());
		Down = list2;
		List<string> list3 = new List<string>();
		list3.AddRange(Left.Distinct());
		Left = list3;
		List<string> list4 = new List<string>();
		list4.AddRange(Right.Distinct());
		Right = list4;
		Directions = new List<string>[4] { Up, Down, Left, Right };
	}

	public void Validate(EquipmentOptions equipment)
	{
		List<string>[] directions = Directions;
		foreach (List<string> list in directions)
		{
			bool flag = false;
			foreach (string item in list)
			{
				if (item == "Player")
				{
					flag = true;
				}
				if (!equipment.Slots.Contains(item) && item != "Player")
				{
					throw new Exception("Config Error: Paperdoll item " + item + " does not exist in equipment slots!");
				}
			}
			if (!flag)
			{
				throw new Exception($"Config Error: Paperdoll direction {list} does not have Player listed!");
			}
		}
	}
}
