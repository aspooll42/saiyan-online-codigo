using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Intersect.Config;

public class EquipmentOptions
{
	public int WeaponSlot;

	public PaperdollOptions Paperdoll { get; set; } = new PaperdollOptions();

	public int ShieldSlot { get; set; } = 3;

	public List<string> Slots { get; set; }

	public List<string> ToolTypes { get; set; }

	[OnDeserializing]
	internal void OnDeserializingMethod(StreamingContext context)
	{
		Slots.Clear();
		ToolTypes.Clear();
	}

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		Validate();
	}

	public void Validate()
	{
		List<string> list = new List<string>();
		list.AddRange(Slots.Distinct());
		Slots = list;
		List<string> list2 = new List<string>();
		list2.AddRange(ToolTypes.Distinct());
		ToolTypes = list2;
		if (WeaponSlot < -1 || WeaponSlot > Slots.Count - 1)
		{
			throw new Exception("Config Error: (WeaponSlot) was out of bounds!");
		}
		if (ShieldSlot < -1 || ShieldSlot > Slots.Count - 1)
		{
			throw new Exception("Config Error: (ShieldSlot) was out of bounds!");
		}
		Paperdoll.Validate(this);
	}

	public EquipmentOptions()
	{
		int num = 5;
		List<string> list = new List<string>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<string> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = "Helmet";
		num2++;
		span[num2] = "Armor";
		num2++;
		span[num2] = "Weapon";
		num2++;
		span[num2] = "Shield";
		num2++;
		span[num2] = "Boots";
		num2++;
		Slots = list;
		num2 = 4;
		List<string> list2 = new List<string>(num2);
		CollectionsMarshal.SetCount(list2, num2);
		span = CollectionsMarshal.AsSpan(list2);
		num = 0;
		span[num] = "Axe";
		num++;
		span[num] = "Pickaxe";
		num++;
		span[num] = "Shovel";
		num++;
		span[num] = "Fishing Rod";
		num++;
		ToolTypes = list2;
		WeaponSlot = 2;
	}
}
