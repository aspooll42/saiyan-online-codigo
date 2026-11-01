using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Intersect.Enums;
using Intersect.GameObjects.Ranges;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Items;

public class EquipmentProperties
{
	[Key]
	public Guid DescriptorId { get; set; }

	[ForeignKey("DescriptorId")]
	[System.Text.Json.Serialization.JsonIgnore]
	[Newtonsoft.Json.JsonIgnore]
	public ItemDescriptor Descriptor { get; set; }

	[NotMapped]
	public Dictionary<Stat, ItemRange> StatRanges { get; set; } = new Dictionary<Stat, ItemRange>();

	[Newtonsoft.Json.JsonIgnore]
	public ItemRange StatRange_Attack
	{
		get
		{
			if (!StatRanges.TryGetValue(Stat.Attack, out var value))
			{
				return StatRange_Attack = new ItemRange();
			}
			return value;
		}
		set
		{
			StatRanges[Stat.Attack] = value;
		}
	}

	[Newtonsoft.Json.JsonIgnore]
	public ItemRange StatRange_AbilityPower
	{
		get
		{
			if (!StatRanges.TryGetValue(Stat.AbilityPower, out var value))
			{
				return StatRange_AbilityPower = new ItemRange();
			}
			return value;
		}
		set
		{
			StatRanges[Stat.AbilityPower] = value;
		}
	}

	[Newtonsoft.Json.JsonIgnore]
	public ItemRange StatRange_Defense
	{
		get
		{
			if (!StatRanges.TryGetValue(Stat.Defense, out var value))
			{
				return StatRange_Defense = new ItemRange();
			}
			return value;
		}
		set
		{
			StatRanges[Stat.Defense] = value;
		}
	}

	[Newtonsoft.Json.JsonIgnore]
	public ItemRange StatRange_MagicResist
	{
		get
		{
			if (!StatRanges.TryGetValue(Stat.MagicResist, out var value))
			{
				return StatRange_MagicResist = new ItemRange();
			}
			return value;
		}
		set
		{
			StatRanges[Stat.MagicResist] = value;
		}
	}

	[Newtonsoft.Json.JsonIgnore]
	public ItemRange StatRange_Speed
	{
		get
		{
			if (!StatRanges.TryGetValue(Stat.Speed, out var value))
			{
				return StatRange_Speed = new ItemRange();
			}
			return value;
		}
		set
		{
			StatRanges[Stat.Speed] = value;
		}
	}

	public EquipmentProperties()
	{
	}

	public EquipmentProperties(ItemDescriptor descriptor)
	{
		Descriptor = descriptor;
	}
}
