using System;
using System.Collections.Generic;
using MessagePack;

namespace Intersect.Framework.Core.GameObjects.Achievements;

[MessagePackObject(false)]
public class AchievementProgress
{
	[Key(0)]
	public string Id { get; set; } = string.Empty;

	[Key(1)]
	public string Name { get; set; } = string.Empty;

	[Key(2)]
	public string Description { get; set; } = string.Empty;

	[Key(3)]
	public string Category { get; set; } = string.Empty;

	[Key(4)]
	public int Points { get; set; }

	[Key(5)]
	public long Objective { get; set; }

	[Key(6)]
	public long Value { get; set; }

	[Key(7)]
	public bool Completed { get; set; }

	[Key(8)]
	public long ExperienceReward { get; set; }

	[Key(9)]
	public List<AchievementRewardItem> ItemRewards { get; set; } = new List<AchievementRewardItem>();

	[Key(10)]
	public Guid TitleRewardId { get; set; }

	[Key(11)]
	public Guid OrnamentRewardId { get; set; }

	[Key(12)]
	public string Chain { get; set; } = string.Empty;

	[Key(13)]
	public string ChainName { get; set; } = string.Empty;
}
