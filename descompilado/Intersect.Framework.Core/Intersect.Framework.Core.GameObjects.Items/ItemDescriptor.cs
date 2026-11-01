using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Animations;
using Intersect.Framework.Core.GameObjects.Conditions;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Framework.Core.GameObjects.Housing;
using Intersect.GameObjects;
using Intersect.GameObjects.Ranges;
using Intersect.Models;
using Intersect.Utilities;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Items;

public class ItemDescriptor : DatabaseObject<ItemDescriptor>, IFolderable
{
	[NotMapped]
	public ConditionLists UsageRequirements { get; set; } = new ConditionLists();

	public string CannotUseMessage { get; set; } = string.Empty;

	[Column("Animation")]
	public Guid AnimationId { get; set; }

	[NotMapped]
	[JsonIgnore]
	public AnimationDescriptor Animation
	{
		get
		{
			return DatabaseObject<AnimationDescriptor>.Get(AnimationId);
		}
		set
		{
			AnimationId = value?.Id ?? Guid.Empty;
		}
	}

	[Column("AttackAnimation")]
	[JsonProperty]
	public Guid AttackAnimationId { get; set; }

	[NotMapped]
	[JsonIgnore]
	public AnimationDescriptor AttackAnimation
	{
		get
		{
			return DatabaseObject<AnimationDescriptor>.Get(AttackAnimationId);
		}
		set
		{
			AttackAnimationId = value?.Id ?? Guid.Empty;
		}
	}

	[Column("EquipmentAnimation")]
	public Guid EquipmentAnimationId { get; set; }

	[NotMapped]
	[JsonIgnore]
	public AnimationDescriptor EquipmentAnimation
	{
		get
		{
			return DatabaseObject<AnimationDescriptor>.Get(EquipmentAnimationId);
		}
		set
		{
			EquipmentAnimationId = value?.Id ?? Guid.Empty;
		}
	}

	[Column("Bound")]
	public bool CanDrop { get; set; } = true;

	public int DropChanceOnDeath { get; set; }

	public bool CanTrade { get; set; } = true;

	public bool CanSell { get; set; } = true;

	public bool CanBank { get; set; } = true;

	public bool CanGuildBank { get; set; } = true;

	public bool CanBag { get; set; } = true;

	public int CritChance { get; set; }

	public double CritMultiplier { get; set; } = 1.5;

	public int Cooldown { get; set; }

	public string? CooldownGroup { get; set; }

	public bool IgnoreGlobalCooldown { get; set; }

	public bool IgnoreCooldownReduction { get; set; }

	public int Damage { get; set; }

	public int DamageType { get; set; }

	public int AttackSpeedModifier { get; set; }

	public int AttackSpeedValue { get; set; }

	public string WeaponSpriteOverride { get; set; }

	public ConsumableData Consumable { get; set; }

	public int EquipmentSlot { get; set; }

	public bool TwoHanded { get; set; }

	public int SlotCount { get; set; }

	[Column("Spell")]
	[JsonProperty]
	public Guid SpellId { get; set; }

	[NotMapped]
	[JsonIgnore]
	public SpellDescriptor Spell
	{
		get
		{
			return DatabaseObject<SpellDescriptor>.Get(SpellId);
		}
		set
		{
			SpellId = value?.Id ?? Guid.Empty;
		}
	}

	public bool QuickCast { get; set; }

	[Column("DestroySpell")]
	public bool SingleUse { get; set; } = true;

	[Column("Event")]
	[JsonProperty]
	public Guid EventId { get; set; }

	[NotMapped]
	[JsonIgnore]
	public EventDescriptor Event
	{
		get
		{
			return DatabaseObject<EventDescriptor>.Get(EventId);
		}
		set
		{
			EventId = value?.Id ?? Guid.Empty;
		}
	}

	public string Description { get; set; } = string.Empty;

	public string FemalePaperdoll { get; set; } = string.Empty;

	public ItemType ItemType { get; set; }

	public string MalePaperdoll { get; set; } = string.Empty;

	public string Icon { get; set; } = string.Empty;

	[Column("Color")]
	[JsonIgnore]
	public string JsonColor
	{
		get
		{
			return JsonConvert.SerializeObject(Color);
		}
		set
		{
			Color = ((!string.IsNullOrWhiteSpace(value)) ? JsonConvert.DeserializeObject<Color>(value) : Color.White);
		}
	}

	[NotMapped]
	public Color Color { get; set; }

	public int Price { get; set; }

	public int Poids { get; set; } = -1;

	public int Rarity { get; set; }

	[Column("Projectile")]
	[JsonProperty]
	public Guid ProjectileId { get; set; }

	[NotMapped]
	[JsonIgnore]
	public ProjectileDescriptor Projectile
	{
		get
		{
			return DatabaseObject<ProjectileDescriptor>.Get(ProjectileId);
		}
		set
		{
			ProjectileId = value?.Id ?? Guid.Empty;
		}
	}

	public int Scaling { get; set; }

	public int ScalingStat { get; set; }

	public int Speed { get; set; }

	public bool Stackable { get; set; }

	public int MaxInventoryStack { get; set; } = 1000000;

	public int MaxBankStack { get; set; } = 1000000;

	public int Tool { get; set; } = -1;

	public int BlockChance { get; set; }

	public int BlockAmount { get; set; }

	public int BlockAbsorption { get; set; }

	public long DespawnTime { get; set; }

	[Column("VitalsGiven")]
	[JsonIgnore]
	public string VitalsJson
	{
		get
		{
			return DatabaseUtils.SaveLongArray(VitalsGiven, Enum.GetValues<Vital>().Length);
		}
		set
		{
			VitalsGiven = DatabaseUtils.LoadLongArray(value, Enum.GetValues<Vital>().Length);
		}
	}

	[NotMapped]
	public long[] VitalsGiven { get; set; }

	[Column("VitalsRegen")]
	[JsonIgnore]
	public string VitalsRegenJson
	{
		get
		{
			return DatabaseUtils.SaveLongArray(VitalsRegen, Enum.GetValues<Vital>().Length);
		}
		set
		{
			VitalsRegen = DatabaseUtils.LoadLongArray(value, Enum.GetValues<Vital>().Length);
		}
	}

	[NotMapped]
	public long[] VitalsRegen { get; set; }

	[Column("PercentageVitalsGiven")]
	[JsonIgnore]
	public string PercentageVitalsJson
	{
		get
		{
			return DatabaseUtils.SaveIntArray(PercentageVitalsGiven, Enum.GetValues<Vital>().Length);
		}
		set
		{
			PercentageVitalsGiven = DatabaseUtils.LoadIntArray(value, Enum.GetValues<Vital>().Length);
		}
	}

	[NotMapped]
	public int[] PercentageVitalsGiven { get; set; }

	[Column("StatsGiven")]
	[JsonIgnore]
	public string StatsJson
	{
		get
		{
			return DatabaseUtils.SaveIntArray(StatsGiven, Enum.GetValues<Stat>().Length);
		}
		set
		{
			StatsGiven = DatabaseUtils.LoadIntArray(value, Enum.GetValues<Stat>().Length);
		}
	}

	[NotMapped]
	public int[] StatsGiven { get; set; }

	[Column("PercentageStatsGiven")]
	[JsonIgnore]
	public string PercentageStatsJson
	{
		get
		{
			return DatabaseUtils.SaveIntArray(PercentageStatsGiven, Enum.GetValues<Stat>().Length);
		}
		set
		{
			PercentageStatsGiven = DatabaseUtils.LoadIntArray(value, Enum.GetValues<Stat>().Length);
		}
	}

	[NotMapped]
	public int[] PercentageStatsGiven { get; set; }

	[Column("UsageRequirements")]
	[JsonIgnore]
	public string JsonUsageRequirements
	{
		get
		{
			return UsageRequirements.Data();
		}
		set
		{
			UsageRequirements.Load(value);
		}
	}

	[JsonIgnore]
	[NotMapped]
	public bool IsStackable
	{
		get
		{
			if ((ItemType == ItemType.Currency || Stackable) && ItemType != ItemType.Equipment)
			{
				return ItemType != ItemType.Bag;
			}
			return false;
		}
	}

	[NotMapped]
	public List<EffectData> Effects { get; set; }

	[Column("Effects")]
	[JsonIgnore]
	public string EffectsJson
	{
		get
		{
			return JsonConvert.SerializeObject(Effects);
		}
		set
		{
			Effects = JsonConvert.DeserializeObject<List<EffectData>>(value ?? "") ?? new List<EffectData>();
		}
	}

	public EquipmentProperties? EquipmentProperties { get; set; }

	[Column("EventTriggers")]
	public string EventTriggersJson
	{
		get
		{
			return JsonConvert.SerializeObject(EventTriggers);
		}
		set
		{
			EventTriggers = JsonConvert.DeserializeObject<Dictionary<ItemEventTrigger, Guid>>(value ?? string.Empty) ?? new Dictionary<ItemEventTrigger, Guid>();
		}
	}

	[NotMapped]
	[JsonIgnore]
	public Dictionary<ItemEventTrigger, Guid> EventTriggers { get; set; } = new Dictionary<ItemEventTrigger, Guid>();

	[NotMapped]
	public FurnitureDescriptor? Furniture { get; set; }

	[Column("Furniture")]
	[JsonIgnore]
	public string? FurnitureJson
	{
		get
		{
			if (Furniture != null)
			{
				return JsonConvert.SerializeObject(Furniture);
			}
			return null;
		}
		set
		{
			Furniture = (string.IsNullOrWhiteSpace(value) ? null : JsonConvert.DeserializeObject<FurnitureDescriptor>(value));
		}
	}

	[NotMapped]
	[JsonIgnore]
	public ItemEffect[] EffectsEnabled => Effects.Select((EffectData effect) => effect.Type).ToArray();

	public string Folder { get; set; } = string.Empty;

	public ItemDescriptor()
	{
		Initialize();
	}

	[JsonConstructor]
	public ItemDescriptor(Guid id)
		: base(id)
	{
		Initialize();
	}

	public EventDescriptor? GetEventTrigger(ItemEventTrigger eventTrigger)
	{
		if (!EventTriggers.TryGetValue(eventTrigger, out var value))
		{
			return null;
		}
		return DatabaseObject<EventDescriptor>.Get(value);
	}

	public bool TryGetRangeFor(Stat stat, [NotNullWhen(true)] out ItemRange? range)
	{
		range = null;
		EquipmentProperties?.StatRanges?.TryGetValue(stat, out range);
		return range != null;
	}

	public void ModifyStatRangeHigh(Stat stat, int val)
	{
		if (EquipmentProperties == null)
		{
			EquipmentProperties equipmentProperties = (EquipmentProperties = new EquipmentProperties());
		}
		if (EquipmentProperties.StatRanges.TryGetValue(stat, out var value))
		{
			value.HighRange = val;
			return;
		}
		ItemRange value2 = new ItemRange(0, val);
		EquipmentProperties.StatRanges[stat] = value2;
	}

	public void ModifyStatRangeLow(Stat stat, int val)
	{
		if (EquipmentProperties == null)
		{
			EquipmentProperties equipmentProperties = (EquipmentProperties = new EquipmentProperties());
		}
		if (EquipmentProperties.StatRanges.TryGetValue(stat, out var value))
		{
			value.LowRange = val;
			return;
		}
		ItemRange value2 = new ItemRange(val, 0);
		EquipmentProperties.StatRanges[stat] = value2;
	}

	public void ValidateStatRanges()
	{
		Dictionary<Stat, ItemRange>.ValueCollection valueCollection = EquipmentProperties?.StatRanges?.Values;
		if (valueCollection == null)
		{
			return;
		}
		foreach (ItemRange item in valueCollection)
		{
			item.Validate();
		}
	}

	public int GetEffectPercentage(ItemEffect type)
	{
		return Effects.Find((EffectData effect) => effect.Type == type)?.Percentage ?? 0;
	}

	public void SetEffectOfType(ItemEffect type, int value)
	{
		EffectData effectData = Effects.Find((EffectData effect) => effect.Type == type);
		if (effectData != null)
		{
			effectData.Percentage = value;
		}
	}

	public static ItemDescriptor[] GetCooldownGroup(string cooldownGroup)
	{
		cooldownGroup = cooldownGroup.Trim();
		if (string.IsNullOrWhiteSpace(cooldownGroup))
		{
			return Array.Empty<ItemDescriptor>();
		}
		return (from descriptor in DatabaseObject<ItemDescriptor>.Lookup.Values.OfType<ItemDescriptor>()
			where descriptor.CooldownGroup?.Trim() == cooldownGroup
			select descriptor).ToArray();
	}

	public string GetPaperdollForGender(Gender gender)
	{
		if (gender == Gender.Male)
		{
			return MalePaperdoll;
		}
		return FemalePaperdoll;
	}

	public override void Load(string json, bool keepCreationTime = false)
	{
		base.Load(json, keepCreationTime);
		if (EquipmentProperties != null)
		{
			EquipmentProperties.Descriptor = this;
			if (EquipmentProperties.DescriptorId != base.Id)
			{
				EquipmentProperties.DescriptorId = default(Guid);
			}
		}
	}

	private void Initialize()
	{
		base.Name = "New Item";
		Speed = 10;
		StatsGiven = new int[Enum.GetValues<Stat>().Length];
		PercentageStatsGiven = new int[Enum.GetValues<Stat>().Length];
		VitalsGiven = new long[Enum.GetValues<Vital>().Length];
		VitalsRegen = new long[Enum.GetValues<Vital>().Length];
		PercentageVitalsGiven = new int[Enum.GetValues<Vital>().Length];
		Consumable = new ConsumableData();
		Effects = new List<EffectData>();
		Color = new Color(255, 255, 255, 255);
	}
}
