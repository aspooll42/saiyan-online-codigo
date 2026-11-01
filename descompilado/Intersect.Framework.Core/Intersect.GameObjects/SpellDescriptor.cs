using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Animations;
using Intersect.Framework.Core.GameObjects.Conditions;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Models;
using Intersect.Utilities;
using Newtonsoft.Json;

namespace Intersect.GameObjects;

public class SpellDescriptor : DatabaseObject<SpellDescriptor>, IFolderable
{
	private long[] _vitalCost = new long[Enum.GetValues<Vital>().Length];

	[NotMapped]
	public long[] VitalCost
	{
		get
		{
			return _vitalCost;
		}
		set
		{
			_vitalCost = value;
		}
	}

	public SpellType SpellType { get; set; }

	public string Description { get; set; } = string.Empty;

	public string Icon { get; set; } = string.Empty;

	[Column("CastAnimation")]
	public Guid CastAnimationId { get; set; }

	[NotMapped]
	[JsonIgnore]
	public AnimationDescriptor CastAnimation
	{
		get
		{
			return DatabaseObject<AnimationDescriptor>.Get(CastAnimationId);
		}
		set
		{
			CastAnimationId = value?.Id ?? Guid.Empty;
		}
	}

	[Column("HitAnimation")]
	public Guid HitAnimationId { get; set; }

	[NotMapped]
	[JsonIgnore]
	public AnimationDescriptor HitAnimation
	{
		get
		{
			return DatabaseObject<AnimationDescriptor>.Get(HitAnimationId);
		}
		set
		{
			HitAnimationId = value?.Id ?? Guid.Empty;
		}
	}

	[Column("TickAnimation")]
	public Guid TickAnimationId { get; set; }

	[NotMapped]
	[JsonIgnore]
	public AnimationDescriptor TickAnimation
	{
		get
		{
			return DatabaseObject<AnimationDescriptor>.Get(TickAnimationId);
		}
		set
		{
			TickAnimationId = value?.Id ?? Guid.Empty;
		}
	}

	public int CastDuration { get; set; }

	public int CooldownDuration { get; set; }

	public string CooldownGroup { get; set; } = string.Empty;

	public bool IgnoreGlobalCooldown { get; set; }

	public bool IgnoreCooldownReduction { get; set; }

	public bool Bound { get; set; }

	[Column("CastRequirements")]
	[JsonIgnore]
	public string JsonCastRequirements
	{
		get
		{
			return CastingRequirements.Data();
		}
		set
		{
			CastingRequirements.Load(value);
		}
	}

	[NotMapped]
	public ConditionLists CastingRequirements { get; set; } = new ConditionLists();

	public string CannotCastMessage { get; set; } = string.Empty;

	public string CastSpriteOverride { get; set; }

	public SpellCombatDescriptor Combat { get; set; } = new SpellCombatDescriptor();

	public SpellWarpDescriptor Warp { get; set; } = new SpellWarpDescriptor();

	public SpellDashDescriptor Dash { get; set; } = new SpellDashDescriptor();

	[NotMapped]
	public SpellMasteryDescriptor Mastery { get; set; } = new SpellMasteryDescriptor();

	[Column("Mastery")]
	[JsonIgnore]
	public string MasteryJson
	{
		get
		{
			return JsonConvert.SerializeObject(Mastery);
		}
		set
		{
			Mastery = (string.IsNullOrWhiteSpace(value) ? new SpellMasteryDescriptor() : (JsonConvert.DeserializeObject<SpellMasteryDescriptor>(value) ?? new SpellMasteryDescriptor()));
		}
	}

	[Column("Event")]
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

	[Column("VitalCost")]
	[JsonIgnore]
	public string VitalCostJson
	{
		get
		{
			return DatabaseUtils.SaveLongArray(_vitalCost, Enum.GetValues<Vital>().Length);
		}
		set
		{
			DatabaseUtils.LoadLongArray(ref _vitalCost, value, Enum.GetValues<Vital>().Length);
		}
	}

	public string Folder { get; set; } = string.Empty;

	[JsonConstructor]
	public SpellDescriptor(Guid id)
		: base(id)
	{
		base.Name = "New Spell";
	}

	public SpellDescriptor()
	{
		base.Name = "New Spell";
	}

	public static SpellDescriptor[] GetCooldownGroup(string cooldownGroup)
	{
		cooldownGroup = cooldownGroup.Trim();
		if (string.IsNullOrWhiteSpace(cooldownGroup))
		{
			return Array.Empty<SpellDescriptor>();
		}
		return (from i in DatabaseObject<SpellDescriptor>.Lookup
			where ((SpellDescriptor)i.Value).CooldownGroup.Trim() == cooldownGroup
			select (SpellDescriptor)i.Value).ToArray();
	}
}
