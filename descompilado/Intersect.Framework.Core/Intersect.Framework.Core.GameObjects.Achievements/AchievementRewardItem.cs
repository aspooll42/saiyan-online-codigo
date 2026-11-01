using System;
using MessagePack;

namespace Intersect.Framework.Core.GameObjects.Achievements;

[MessagePackObject(false)]
public class AchievementRewardItem
{
	[Key(0)]
	public Guid ItemId { get; set; }

	[Key(1)]
	public int Quantity { get; set; } = 1;
}
