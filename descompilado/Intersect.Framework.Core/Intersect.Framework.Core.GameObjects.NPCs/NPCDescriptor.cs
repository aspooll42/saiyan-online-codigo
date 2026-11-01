using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Animations;
using Intersect.Framework.Core.GameObjects.Conditions;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.GameObjects;
using Intersect.Models;
using Intersect.Utilities;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.NPCs;

public class NPCDescriptor : DatabaseObject<NPCDescriptor>, IFolderable
{
	private long[] _maxVitals = new long[Enum.GetValues<Vital>().Length];

	private int[] _stats = new int[Enum.GetValues<Stat>().Length];

	private long[] _vitalRegen = new long[Enum.GetValues<Vital>().Length];

	[NotMapped]
	public ConditionLists AttackOnSightConditions { get; set; } = new ConditionLists();

	[NotMapped]
	public List<Drop> Drops { get; set; } = new List<Drop>();

	[NotMapped]
	[JsonIgnore]
	public long[] MaxVitals
	{
		get
		{
			return _maxVitals;
		}
		set
		{
			_maxVitals = value;
		}
	}

	[JsonProperty("MaxVitals")]
	[NotMapped]
	public IReadOnlyDictionary<Vital, long> MaxVitalsLookup
	{
		get
		{
			return MaxVitals.Select((long value, int index) => (value: value, index: index)).ToDictionary(((long value, int index) t) => (Vital)t.index, ((long value, int index) t) => t.value).AsReadOnly();
		}
		set
		{
			foreach (var (vital2, num2) in value)
			{
				MaxVitals[(int)vital2] = num2;
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

	[JsonProperty("Stats")]
	[NotMapped]
	public IReadOnlyDictionary<Stat, int> StatsLookup
	{
		get
		{
			return Stats.Select((int statValue, int index) => (statValue: statValue, index: index)).ToDictionary(((int statValue, int index) t) => (Stat)t.index, ((int statValue, int index) t) => t.statValue).AsReadOnly();
		}
		set
		{
			foreach (var (stat2, num2) in value)
			{
				Stats[(int)stat2] = num2;
			}
		}
	}

	[NotMapped]
	public ConditionLists PlayerCanAttackConditions { get; set; } = new ConditionLists();

	[NotMapped]
	public ConditionLists PlayerFriendConditions { get; set; } = new ConditionLists();

	[NotMapped]
	[JsonIgnore]
	public int[] Stats
	{
		get
		{
			return _stats;
		}
		set
		{
			_stats = value;
		}
	}

	[NotMapped]
	[JsonIgnore]
	public long[] VitalRegen
	{
		get
		{
			return _vitalRegen;
		}
		set
		{
			_vitalRegen = value;
		}
	}

	[NotMapped]
	public List<SpellEffect> Immunities { get; set; } = new List<SpellEffect>();

	[JsonIgnore]
	[Column("Immunities")]
	public string ImmunitiesJson
	{
		get
		{
			return JsonConvert.SerializeObject(Immunities);
		}
		set
		{
			Immunities = JsonConvert.DeserializeObject<List<SpellEffect>>(value ?? "") ?? new List<SpellEffect>();
		}
	}

	[Column("AggroList")]
	[JsonIgnore]
	public string JsonAggroList
	{
		get
		{
			return JsonConvert.SerializeObject(AggroList);
		}
		set
		{
			AggroList = JsonConvert.DeserializeObject<List<Guid>>(value);
		}
	}

	[NotMapped]
	public List<Guid> AggroList { get; set; } = new List<Guid>();

	public bool AttackAllies { get; set; }

	[Column("AttackAnimation")]
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

	public bool Aggressive { get; set; }

	public byte Movement { get; set; }

	public bool Swarm { get; set; }

	public byte FleeHealthPercentage { get; set; }

	public bool FocusHighestDamageDealer { get; set; } = true;

	public int ResetRadius { get; set; }

	public bool Friendly { get; set; }

	public int WanderRadius { get; set; }

	public bool NoticePlayers { get; set; }

	public int NoticeDelay { get; set; } = 800;

	public bool Bleeds { get; set; } = true;

	[NotMapped]
	public NpcBossDescriptor Boss { get; set; } = new NpcBossDescriptor();

	[Column("Boss")]
	[JsonIgnore]
	public string BossJson
	{
		get
		{
			return JsonConvert.SerializeObject(Boss);
		}
		set
		{
			Boss = (string.IsNullOrWhiteSpace(value) ? new NpcBossDescriptor() : (JsonConvert.DeserializeObject<NpcBossDescriptor>(value) ?? new NpcBossDescriptor()));
		}
	}

	public int GuardChance { get; set; }

	public int RangedDistance { get; set; }

	public bool IsGame { get; set; }

	public int HunterLevel { get; set; }

	public int HunterExperience { get; set; }

	[Column("PlayerFriendConditions")]
	[JsonIgnore]
	public string PlayerFriendConditionsJson
	{
		get
		{
			return PlayerFriendConditions.Data();
		}
		set
		{
			PlayerFriendConditions.Load(value);
		}
	}

	[Column("AttackOnSightConditions")]
	[JsonIgnore]
	public string AttackOnSightConditionsJson
	{
		get
		{
			return AttackOnSightConditions.Data();
		}
		set
		{
			AttackOnSightConditions.Load(value);
		}
	}

	[Column("PlayerCanAttackConditions")]
	[JsonIgnore]
	public string PlayerCanAttackConditionsJson
	{
		get
		{
			return PlayerCanAttackConditions.Data();
		}
		set
		{
			PlayerCanAttackConditions.Load(value);
		}
	}

	public int Damage { get; set; } = 1;

	public int DamageType { get; set; }

	public int CritChance { get; set; }

	public double CritMultiplier { get; set; } = 1.5;

	public double Tenacity { get; set; }

	public int AttackSpeedModifier { get; set; }

	public int AttackSpeedValue { get; set; }

	[Column("OnDeathEvent")]
	public Guid OnDeathEventId { get; set; }

	[NotMapped]
	[JsonIgnore]
	public EventDescriptor OnDeathEvent
	{
		get
		{
			return DatabaseObject<EventDescriptor>.Get(OnDeathEventId);
		}
		set
		{
			OnDeathEventId = value?.Id ?? Guid.Empty;
		}
	}

	[Column("OnDeathPartyEvent")]
	public Guid OnDeathPartyEventId { get; set; }

	[NotMapped]
	[JsonIgnore]
	public EventDescriptor OnDeathPartyEvent
	{
		get
		{
			return DatabaseObject<EventDescriptor>.Get(OnDeathPartyEventId);
		}
		set
		{
			OnDeathPartyEventId = value?.Id ?? Guid.Empty;
		}
	}

	[Column("Drops")]
	[JsonIgnore]
	public string JsonDrops
	{
		get
		{
			return JsonConvert.SerializeObject(Drops);
		}
		set
		{
			Drops = JsonConvert.DeserializeObject<List<Drop>>(value);
		}
	}

	public bool IndividualizedLoot { get; set; }

	public long Experience { get; set; }

	public int Level { get; set; } = 1;

	[Column("MaxVital")]
	[JsonIgnore]
	public string JsonMaxVital
	{
		get
		{
			return DatabaseUtils.SaveLongArray(_maxVitals, Enum.GetValues<Vital>().Length);
		}
		set
		{
			DatabaseUtils.LoadLongArray(ref _maxVitals, value, Enum.GetValues<Vital>().Length);
		}
	}

	public bool NpcVsNpcEnabled { get; set; }

	public int Scaling { get; set; } = 100;

	public int ScalingStat { get; set; }

	public int SightRange { get; set; }

	public int SpawnDuration { get; set; }

	public int SpellFrequency { get; set; } = 2;

	[JsonIgnore]
	[Column("Spells")]
	public string CraftsJson
	{
		get
		{
			return JsonConvert.SerializeObject(Spells, Formatting.None);
		}
		protected set
		{
			Spells = JsonConvert.DeserializeObject<DbList<SpellDescriptor>>(value);
		}
	}

	[NotMapped]
	public DbList<SpellDescriptor> Spells { get; set; } = new DbList<SpellDescriptor>();

	public string Sprite { get; set; } = string.Empty;

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
	public Color Color { get; set; } = new Color(255, 255, 255, 255);

	[Column("Stats")]
	[JsonIgnore]
	public string JsonStat
	{
		get
		{
			return DatabaseUtils.SaveIntArray(_stats, Enum.GetValues<Stat>().Length);
		}
		set
		{
			DatabaseUtils.LoadIntArray(ref _stats, value, Enum.GetValues<Stat>().Length);
		}
	}

	[JsonIgnore]
	[Column("VitalRegen")]
	public string RegenJson
	{
		get
		{
			return DatabaseUtils.SaveLongArray(_vitalRegen, Enum.GetValues<Vital>().Length);
		}
		set
		{
			DatabaseUtils.LoadLongArray(ref _vitalRegen, value, Enum.GetValues<Vital>().Length);
		}
	}

	public string Folder { get; set; } = string.Empty;

	[JsonConstructor]
	public NPCDescriptor(Guid id)
		: base(id)
	{
		base.Name = "New Npc";
	}

	public NPCDescriptor()
	{
		base.Name = "New Npc";
	}

	public SpellDescriptor GetRandomSpell(Random random)
	{
		if (Spells == null || Spells.Count == 0)
		{
			return null;
		}
		int index = random.Next(0, Spells.Count);
		Guid id = Spells[index];
		return DatabaseObject<SpellDescriptor>.Get(id);
	}
}
