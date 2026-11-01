using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Animations;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Models;
using Intersect.Server.Utilities;
using Intersect.Utilities;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.PlayerClass;

public class ClassDescriptor : DatabaseObject<ClassDescriptor>, IFolderable
{
	public const long DEFAULT_BASE_EXPERIENCE = 100L;

	public const long DEFAULT_EXPERIENCE_INCREASE = 50L;

	[JsonIgnore]
	private long mBaseExp;

	[JsonIgnore]
	private long mExpIncrease;

	[NotMapped]
	[JsonIgnore]
	public int[] BaseStat { get; set; } = new int[Enum.GetValues<Stat>().Length];

	[NotMapped]
	[JsonIgnore]
	public long[] BaseVital { get; set; } = new long[Enum.GetValues<Vital>().Length];

	[JsonProperty("BaseVital")]
	[NotMapped]
	public IReadOnlyDictionary<Vital, long> BaseVitalLookup
	{
		get
		{
			return BaseVital.Select((long value, int index) => (value: value, index: index)).ToDictionary(((long value, int index) t) => (Vital)t.index, ((long value, int index) t) => t.value).AsReadOnly();
		}
		set
		{
			foreach (var (vital2, num2) in value)
			{
				BaseVital[(int)vital2] = num2;
			}
		}
	}

	[JsonProperty("VitalIncrease")]
	[NotMapped]
	public IReadOnlyDictionary<Vital, long> VitalIncreaseLookup
	{
		get
		{
			return VitalIncrease.Select((long value, int index) => (value: value, index: index)).ToDictionary(((long value, int index) t) => (Vital)t.index, ((long value, int index) t) => t.value).AsReadOnly();
		}
		set
		{
			foreach (var (vital2, num2) in value)
			{
				VitalIncrease[(int)vital2] = num2;
			}
		}
	}

	[JsonProperty("VitalRegen")]
	[NotMapped]
	public IReadOnlyDictionary<Vital, long> VitalRegenLookup
	{
		get
		{
			return VitalRegen.Select((long value, int index) => (value: value, index: index)).ToDictionary(((long value, int index) t) => (Vital)t.index, ((long value, int index) t) => t.value).AsReadOnly();
		}
		set
		{
			foreach (var (vital2, num2) in value)
			{
				VitalRegen[(int)vital2] = num2;
			}
		}
	}

	[JsonProperty("BaseStat")]
	[NotMapped]
	public IReadOnlyDictionary<Stat, int> BaseStatLookup
	{
		get
		{
			return BaseStat.Select((int statValue, int index) => (statValue: statValue, index: index)).ToDictionary(((int statValue, int index) t) => (Stat)t.index, ((int statValue, int index) t) => t.statValue).AsReadOnly();
		}
		set
		{
			foreach (var (stat2, num2) in value)
			{
				BaseStat[(int)stat2] = num2;
			}
		}
	}

	[JsonProperty("StatIncrease")]
	[NotMapped]
	public IReadOnlyDictionary<Stat, int> StatIncreaseLookup
	{
		get
		{
			return StatIncrease.Select((int statValue, int index) => (statValue: statValue, index: index)).ToDictionary(((int statValue, int index) t) => (Stat)t.index, ((int statValue, int index) t) => t.statValue).AsReadOnly();
		}
		set
		{
			foreach (var (stat2, num2) in value)
			{
				StatIncrease[(int)stat2] = num2;
			}
		}
	}

	[NotMapped]
	public Dictionary<int, long> ExperienceOverrides { get; set; } = new Dictionary<int, long>();

	[NotMapped]
	public List<ClassItem> Items { get; set; } = new List<ClassItem>();

	[NotMapped]
	public List<ClassSpell> Spells { get; set; } = new List<ClassSpell>();

	[NotMapped]
	public List<ClassSprite> Sprites { get; set; } = new List<ClassSprite>();

	[NotMapped]
	[JsonIgnore]
	public int[] StatIncrease { get; set; } = new int[Enum.GetValues<Stat>().Length];

	[NotMapped]
	[JsonIgnore]
	public long[] VitalIncrease { get; set; } = new long[Enum.GetValues<Vital>().Length];

	[NotMapped]
	[JsonIgnore]
	public long[] VitalRegen { get; set; } = new long[Enum.GetValues<Vital>().Length];

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

	public int BasePoints { get; set; }

	public int CritChance { get; set; }

	public double CritMultiplier { get; set; } = 1.5;

	public int Damage { get; set; } = 1;

	public int DamageType { get; set; }

	public int AttackSpeedModifier { get; set; }

	public int AttackSpeedValue { get; set; }

	public string AttackSpriteOverride { get; set; }

	public long BaseExp
	{
		get
		{
			return mBaseExp;
		}
		set
		{
			mBaseExp = Math.Max(0L, value);
			ExperienceCurve.BaseExperience = Math.Max(1L, mBaseExp);
		}
	}

	public long ExpIncrease
	{
		get
		{
			return mExpIncrease;
		}
		set
		{
			mExpIncrease = Math.Max(0L, value);
			ExperienceCurve.Gain = 1.0 + (double)value / 100.0;
		}
	}

	public bool IncreasePercentage { get; set; }

	[JsonIgnore]
	[NotMapped]
	public ExperienceCurve ExperienceCurve { get; }

	public bool Locked { get; set; }

	public int PointIncrease { get; set; }

	public int Scaling { get; set; } = 100;

	public int ScalingStat { get; set; }

	[Column("SpawnMap")]
	[JsonProperty]
	public Guid SpawnMapId { get; set; }

	[NotMapped]
	[JsonIgnore]
	public MapDescriptor SpawnMap
	{
		get
		{
			return DatabaseObject<MapDescriptor>.Get(SpawnMapId);
		}
		set
		{
			SpawnMapId = value?.Id ?? Guid.Empty;
		}
	}

	public int SpawnX { get; set; }

	public int SpawnY { get; set; }

	public int SpawnDir { get; set; }

	[Column("BaseStats")]
	[JsonIgnore]
	public string JsonBaseStats
	{
		get
		{
			return DatabaseUtils.SaveIntArray(BaseStat, Enum.GetValues<Stat>().Length);
		}
		set
		{
			BaseStat = DatabaseUtils.LoadIntArray(value, Enum.GetValues<Stat>().Length);
		}
	}

	[Column("BaseVitals")]
	[JsonIgnore]
	public string JsonBaseVitals
	{
		get
		{
			return DatabaseUtils.SaveLongArray(BaseVital, Enum.GetValues<Vital>().Length);
		}
		set
		{
			BaseVital = DatabaseUtils.LoadLongArray(value, Enum.GetValues<Vital>().Length);
		}
	}

	[Column("Items")]
	[JsonIgnore]
	public string JsonItems
	{
		get
		{
			return JsonConvert.SerializeObject(Items);
		}
		protected set
		{
			Items = JsonConvert.DeserializeObject<List<ClassItem>>(value);
		}
	}

	[Column("Spells")]
	[JsonIgnore]
	public string JsonSpells
	{
		get
		{
			return JsonConvert.SerializeObject(Spells);
		}
		protected set
		{
			Spells = JsonConvert.DeserializeObject<List<ClassSpell>>(value);
		}
	}

	[JsonIgnore]
	[Column("Sprites")]
	public string JsonSprites
	{
		get
		{
			return JsonConvert.SerializeObject(Sprites);
		}
		protected set
		{
			Sprites = JsonConvert.DeserializeObject<List<ClassSprite>>(value);
		}
	}

	[JsonIgnore]
	[Column("StatIncreases")]
	public string StatIncreaseJson
	{
		get
		{
			return DatabaseUtils.SaveIntArray(StatIncrease, Enum.GetValues<Stat>().Length);
		}
		set
		{
			StatIncrease = DatabaseUtils.LoadIntArray(value, Enum.GetValues<Stat>().Length);
		}
	}

	[JsonIgnore]
	[Column("VitalIncreases")]
	public string VitalIncreaseJson
	{
		get
		{
			return DatabaseUtils.SaveLongArray(VitalIncrease, Enum.GetValues<Vital>().Length);
		}
		set
		{
			VitalIncrease = DatabaseUtils.LoadLongArray(value, Enum.GetValues<Vital>().Length);
		}
	}

	[JsonIgnore]
	[Column("VitalRegen")]
	public string RegenJson
	{
		get
		{
			return DatabaseUtils.SaveLongArray(VitalRegen, Enum.GetValues<Vital>().Length);
		}
		set
		{
			VitalRegen = DatabaseUtils.LoadLongArray(value, Enum.GetValues<Vital>().Length);
		}
	}

	[JsonIgnore]
	[Column("ExperienceOverrides")]
	public string ExpOverridesJson
	{
		get
		{
			return JsonConvert.SerializeObject(ExperienceOverrides);
		}
		set
		{
			ExperienceOverrides = JsonConvert.DeserializeObject<Dictionary<int, long>>(value ?? "");
			if (ExperienceOverrides == null)
			{
				ExperienceOverrides = new Dictionary<int, long>();
			}
		}
	}

	public string Folder { get; set; } = string.Empty;

	[JsonConstructor]
	public ClassDescriptor(Guid id)
		: base(id)
	{
		base.Name = "New Class";
		ExperienceCurve = new ExperienceCurve();
		ExperienceCurve.Calculate(1);
		BaseExp = 100L;
		ExpIncrease = 50L;
	}

	public ClassDescriptor()
	{
		base.Name = "New Class";
		ExperienceCurve = new ExperienceCurve();
		ExperienceCurve.Calculate(1);
		BaseExp = 100L;
		ExpIncrease = 50L;
	}

	public long ExperienceToNextLevel(int level)
	{
		if (ExperienceOverrides.ContainsKey(level))
		{
			return ExperienceOverrides[level];
		}
		return ExperienceCurve.Calculate(level);
	}
}
