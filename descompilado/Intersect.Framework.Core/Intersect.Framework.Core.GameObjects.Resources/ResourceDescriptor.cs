using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Framework.Core.GameObjects.Animations;
using Intersect.Framework.Core.GameObjects.Conditions;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Resources;

public class ResourceDescriptor : DatabaseObject<ResourceDescriptor>, IFolderable
{
	[NotMapped]
	public List<Drop> Drops { get; set; } = new List<Drop>();

	[NotMapped]
	public ConditionLists HarvestingRequirements { get; set; } = new ConditionLists();

	public string CannotHarvestMessage { get; set; } = string.Empty;

	public bool UseExplicitMaxHealthForResourceStates { get; set; }

	[NotMapped]
	[JsonIgnore]
	public Dictionary<Guid, ResourceStateDescriptor> States { get; set; } = new Dictionary<Guid, ResourceStateDescriptor>();

	[Column("States")]
	public string JsonStates
	{
		get
		{
			return JsonConvert.SerializeObject(States);
		}
		set
		{
			States = JsonConvert.DeserializeObject<Dictionary<Guid, ResourceStateDescriptor>>(value);
		}
	}

	[Column("DeathAnimation")]
	public Guid DeathAnimationId { get; set; }

	[NotMapped]
	[JsonIgnore]
	public AnimationDescriptor DeathAnimation
	{
		get
		{
			return DatabaseObject<AnimationDescriptor>.Get(DeathAnimationId);
		}
		set
		{
			DeathAnimationId = value?.Id ?? Guid.Empty;
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

	[Column("HarvestingRequirements")]
	[JsonIgnore]
	public string JsonHarvestingRequirements
	{
		get
		{
			return HarvestingRequirements.Data();
		}
		set
		{
			HarvestingRequirements.Load(value);
		}
	}

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

	public int VitalRegen { get; set; }

	public int MaxHp { get; set; }

	public int MinHp { get; set; }

	public int SpawnDuration { get; set; }

	public int Tool { get; set; } = -1;

	public int ProfessionLevel { get; set; }

	public int ProfessionExperience { get; set; }

	public bool WalkableAfter { get; set; }

	public bool WalkableBefore { get; set; }

	public string Folder { get; set; } = string.Empty;

	[JsonConstructor]
	public ResourceDescriptor(Guid id)
		: base(id)
	{
		base.Name = "New Resource";
	}

	public ResourceDescriptor()
	{
		base.Name = "New Resource";
	}
}
