using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Dungeons;

public class DungeonDescriptor : DatabaseObject<DungeonDescriptor>, IFolderable
{
	private List<DungeonDifficultySettings>? _difficulties;

	public string Zone { get; set; } = string.Empty;

	public int LevelMin { get; set; } = 1;

	public int LevelMax { get; set; } = 10;

	public string Image { get; set; } = string.Empty;

	public Guid EntranceMapId { get; set; }

	public int EntranceX { get; set; }

	public int EntranceY { get; set; }

	public int Beans { get; set; } = 3;

	public int SoloHealthPercent { get; set; } = 60;

	public int HealthPercentPerExtraPlayer { get; set; } = 25;

	public long ChestZeni { get; set; }

	public long ChestExperience { get; set; }

	public Guid TitleId { get; set; }

	public int TargetSeconds { get; set; }

	[NotMapped]
	[JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
	public List<DungeonDifficultySettings> Difficulties
	{
		get
		{
			List<DungeonDifficultySettings> difficulties = _difficulties;
			if (difficulties == null || difficulties.Count != 3)
			{
				_difficulties = DungeonRules.DefaultDifficulties(this);
			}
			return _difficulties;
		}
		set
		{
			_difficulties = value;
		}
	}

	[Column("Difficulties")]
	[JsonIgnore]
	public string DifficultiesJson
	{
		get
		{
			return JsonConvert.SerializeObject(Difficulties);
		}
		set
		{
			_difficulties = (string.IsNullOrWhiteSpace(value) ? null : JsonConvert.DeserializeObject<List<DungeonDifficultySettings>>(value));
		}
	}

	[NotMapped]
	[JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
	public List<DungeonRoom> Rooms { get; set; } = new List<DungeonRoom>();

	[Column("Rooms")]
	[JsonIgnore]
	public string RoomsJson
	{
		get
		{
			return JsonConvert.SerializeObject(Rooms);
		}
		set
		{
			Rooms = (string.IsNullOrWhiteSpace(value) ? new List<DungeonRoom>() : (JsonConvert.DeserializeObject<List<DungeonRoom>>(value) ?? new List<DungeonRoom>()));
		}
	}

	[NotMapped]
	[JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
	public List<DungeonReward> Chest { get; set; } = new List<DungeonReward>();

	[Column("Chest")]
	[JsonIgnore]
	public string ChestJson
	{
		get
		{
			return JsonConvert.SerializeObject(Chest);
		}
		set
		{
			Chest = (string.IsNullOrWhiteSpace(value) ? new List<DungeonReward>() : (JsonConvert.DeserializeObject<List<DungeonReward>>(value) ?? new List<DungeonReward>()));
		}
	}

	public string Folder { get; set; } = string.Empty;

	[JsonConstructor]
	public DungeonDescriptor(Guid id)
		: base(id)
	{
		base.Name = "Nouveau donjon";
	}

	public DungeonDescriptor()
	{
		base.Name = "Nouveau donjon";
	}

	public DungeonDifficultySettings Settings(DungeonDifficulty difficulty)
	{
		return Difficulties[Math.Clamp((int)difficulty, 0, 2)];
	}
}
