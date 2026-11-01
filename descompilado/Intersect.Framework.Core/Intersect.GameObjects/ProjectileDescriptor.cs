using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.GameObjects;

public class ProjectileDescriptor : DatabaseObject<ProjectileDescriptor>, IFolderable
{
	public const int MAX_PROJECTILE_DIRECTIONS = 8;

	public static readonly int[] ProjectileRotationDir = new int[64]
	{
		0, 1, 2, 3, 4, 5, 7, 6, 1, 0,
		3, 2, 6, 7, 5, 4, 2, 3, 1, 0,
		7, 4, 6, 5, 3, 2, 0, 1, 5, 6,
		4, 7, 4, 6, 7, 5, 2, 0, 1, 3,
		5, 7, 4, 6, 0, 3, 2, 1, 6, 4,
		5, 7, 3, 1, 0, 2, 7, 5, 6, 4,
		1, 2, 3, 0
	};

	public const int SPAWN_LOCATIONS_HEIGHT = 5;

	public const int SPAWN_LOCATIONS_WIDTH = 5;

	[NotMapped]
	public List<ProjectileAnimation> Animations { get; set; } = new List<ProjectileAnimation>();

	[NotMapped]
	public Location[,] SpawnLocations { get; set; } = new Location[5, 5];

	[Column("Ammo")]
	public Guid AmmoItemId { get; set; } = Guid.Empty;

	[NotMapped]
	[JsonIgnore]
	public ItemDescriptor Ammo
	{
		get
		{
			return DatabaseObject<ItemDescriptor>.Get(AmmoItemId);
		}
		set
		{
			AmmoItemId = value?.Id ?? Guid.Empty;
		}
	}

	public int AmmoRequired { get; set; } = 1;

	[Column("Animations")]
	[JsonIgnore]
	public string AnimationsJson
	{
		get
		{
			return JsonConvert.SerializeObject(Animations);
		}
		set
		{
			Animations = JsonConvert.DeserializeObject<List<ProjectileAnimation>>(value);
		}
	}

	public int Delay { get; set; } = 1;

	public bool GrappleHook { get; set; }

	public bool IgnoreActiveResources { get; set; }

	public bool IgnoreExhaustedResources { get; set; }

	public bool IgnoreMapBlocks { get; set; }

	public bool IgnoreZDimension { get; set; }

	public bool PierceTarget { get; set; }

	public int Knockback { get; set; }

	public int Quantity { get; set; } = 1;

	public int Range { get; set; } = 1;

	public bool HomingBehavior { get; set; }

	public bool DirectShotBehavior { get; set; }

	[Column("SpawnLocations")]
	[JsonIgnore]
	public string SpawnsJson
	{
		get
		{
			return JsonConvert.SerializeObject(SpawnLocations);
		}
		set
		{
			SpawnLocations = JsonConvert.DeserializeObject<Location[,]>(value);
		}
	}

	[NotMapped]
	public List<GrappleOption> GrappleHookOptions { get; set; } = new List<GrappleOption>();

	[JsonIgnore]
	[Column("GrappleHookOptions")]
	public string GrappleHookOptionsJson
	{
		get
		{
			return JsonConvert.SerializeObject(GrappleHookOptions);
		}
		set
		{
			GrappleHookOptions = JsonConvert.DeserializeObject<List<GrappleOption>>(value ?? "") ?? new List<GrappleOption>();
		}
	}

	public int Speed { get; set; } = 1;

	[Column("Spell")]
	public Guid SpellId { get; set; } = Guid.Empty;

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

	public string Folder { get; set; } = string.Empty;

	[JsonConstructor]
	public ProjectileDescriptor(Guid id)
		: base(id)
	{
		base.Name = "New Projectile";
		for (int i = 0; i < 5; i++)
		{
			for (int j = 0; j < 5; j++)
			{
				SpawnLocations[i, j] = new Location();
			}
		}
	}

	public ProjectileDescriptor()
	{
		base.Name = "New Projectile";
		for (int i = 0; i < 5; i++)
		{
			for (int j = 0; j < 5; j++)
			{
				SpawnLocations[i, j] = new Location();
			}
		}
	}
}
