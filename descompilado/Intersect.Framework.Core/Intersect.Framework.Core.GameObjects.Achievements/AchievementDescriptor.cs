using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Framework.Core.GameObjects.Dungeons;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Achievements;

public class AchievementDescriptor : DatabaseObject<AchievementDescriptor>, IFolderable
{
	public string Description { get; set; } = string.Empty;

	public string Category { get; set; } = string.Empty;

	public int Points { get; set; } = 10;

	public AchievementConditionKind ConditionKind { get; set; }

	[NotMapped]
	[JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
	public AchievementDungeonCondition DungeonCondition { get; set; } = new AchievementDungeonCondition();

	[JsonIgnore]
	[Column("DungeonCondition")]
	public string DungeonConditionJson
	{
		get
		{
			return JsonConvert.SerializeObject(DungeonCondition, Formatting.None);
		}
		set
		{
			DungeonCondition = (string.IsNullOrWhiteSpace(value) ? new AchievementDungeonCondition() : (JsonConvert.DeserializeObject<AchievementDungeonCondition>(value) ?? new AchievementDungeonCondition()));
		}
	}

	public Guid ConditionVariableId { get; set; }

	public long Objective { get; set; } = 1L;

	public long ExperienceReward { get; set; }

	[NotMapped]
	public List<AchievementRewardItem> ItemRewards { get; set; } = new List<AchievementRewardItem>();

	[JsonIgnore]
	[Column("ItemRewards")]
	public string ItemRewardsJson
	{
		get
		{
			return JsonConvert.SerializeObject(ItemRewards, Formatting.None);
		}
		protected set
		{
			ItemRewards = JsonConvert.DeserializeObject<List<AchievementRewardItem>>(value ?? "[]") ?? new List<AchievementRewardItem>();
		}
	}

	[Column("TitleReward")]
	public Guid TitleRewardId { get; set; }

	[Column("OrnamentReward")]
	public Guid OrnamentRewardId { get; set; }

	public string Folder { get; set; } = string.Empty;

	[JsonConstructor]
	public AchievementDescriptor(Guid id)
		: base(id)
	{
		base.Name = "New Achievement";
	}

	public AchievementDescriptor()
	{
		base.Name = "New Achievement";
	}
}
